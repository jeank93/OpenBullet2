using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenBullet2.Core.Entities;
using OpenBullet2.Core.Repositories;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenBullet2.Native.ViewModels;

public class WordlistsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory scopeFactory;
    private bool initialized;

    private ObservableCollection<WordlistEntity> wordlistsCollection = [];
    public ObservableCollection<WordlistEntity> WordlistsCollection
    {
        get => wordlistsCollection;
        private set
        {
            wordlistsCollection = value;
            OnPropertyChanged();
        }
    }

    public int Total => WordlistsCollection.Count;

    private string searchString = string.Empty;
    public string SearchString
    {
        get => searchString;
        set
        {
            searchString = value;
            OnPropertyChanged();
            CollectionViewSource.GetDefaultView(WordlistsCollection).Refresh();
            OnPropertyChanged(nameof(Total));
        }
    }

    public WordlistsViewModel(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
        WordlistsCollection = [];
    }

    public async Task InitializeAsync()
    {
        if (!initialized)
        {
            await RefreshListAsync();
            initialized = true;
        }
    }

    private void HookFilters()
    {
        var view = (CollectionView)CollectionViewSource.GetDefaultView(WordlistsCollection);
        view.Filter = WordlistsFilter;
    }

    private bool WordlistsFilter(object item)
        => item is WordlistEntity wordlist
           && (wordlist.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false);

    public WordlistEntity GetWordlistByName(string name) => WordlistsCollection.First(w => w.Name == name);

    public Task AddAsync(WordlistEntity wordlist)
    {
        if (WordlistsCollection.Any(w => w.FileName == wordlist.FileName))
        {
            throw new Exception($"Wordlist already present: {wordlist.FileName}");
        }

        WordlistsCollection.Add(wordlist);
        return WithRepositoryAsync(repo => repo.AddAsync(wordlist));
    }

    public async Task RefreshListAsync()
    {
        var items = await WithRepositoryAsync(repo => repo.GetAll().ToListAsync());
        WordlistsCollection = new ObservableCollection<WordlistEntity>(items);
        HookFilters();
    }

    public async Task UpdateAsync(WordlistEntity wordlist) => await WithRepositoryAsync(repo => repo.UpdateAsync(wordlist));

    public async Task DeleteAsync(WordlistEntity wordlist)
    {
        WordlistsCollection.Remove(wordlist);
        await WithRepositoryAsync(repo => repo.DeleteAsync(wordlist, false));
        OnPropertyChanged(nameof(Total));
    }

    public void DeleteAll()
    {
        WordlistsCollection.Clear();
        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<IWordlistRepository>().Purge();
        OnPropertyChanged(nameof(Total));
    }

    public async Task<int> DeleteNotFoundAsync()
    {
        var deleted = 0;

        for (var i = 0; i < WordlistsCollection.Count; i++)
        {
            var wordlist = WordlistsCollection[i];

            if (string.IsNullOrEmpty(wordlist.FileName) || !File.Exists(wordlist.FileName))
            {
                await DeleteAsync(wordlist);
                deleted++;
                i--;
            }
        }

        return deleted;
    }

    private async Task WithRepositoryAsync(Func<IWordlistRepository, Task> action)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWordlistRepository>();
        await action(repo);
    }

    private async Task<T> WithRepositoryAsync<T>(Func<IWordlistRepository, Task<T>> action)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWordlistRepository>();
        return await action(repo);
    }
}
