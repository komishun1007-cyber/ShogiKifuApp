// File: Data/KifuDatabase.cs
using SQLite;
using ShogiKifuApp.Models;

namespace ShogiKifuApp.Data;

public class KifuDatabase
{
    private const string DbName = "kifu.db3";
    private SQLiteAsyncConnection? _conn;

    private async Task<SQLiteAsyncConnection> GetConnAsync()
    {
        if (_conn != null) return _conn;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, DbName);
        _conn = new SQLiteAsyncConnection(dbPath);
        await _conn.CreateTableAsync<KifuRecord>(); // なければ作成
        return _conn;
    }

    public async Task<int> CountAsync()
    {
        var db = await GetConnAsync();
        return await db.Table<KifuRecord>().CountAsync();
    }

    public async Task<List<KifuRecord>> GetAllAsync()
    {
        var db = await GetConnAsync();
        // 新しい対局が上に来るよう日付降順
        return await db.Table<KifuRecord>()
                       .OrderByDescending(x => x.Date)
                       .ToListAsync();
    }

    public async Task<int> InsertAsync(KifuRecord rec)
    {
        var db = await GetConnAsync();
        return await db.InsertAsync(rec);
    }

    public async Task<int> UpdateAsync(KifuRecord rec)
    {
        var db = await GetConnAsync();
        return await db.UpdateAsync(rec);
    }

    public async Task<int> DeleteAsync(KifuRecord rec)
    {
        var db = await GetConnAsync();
        return await db.DeleteAsync(rec);
    }
}
