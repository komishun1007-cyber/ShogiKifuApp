using SQLite;
using ShogiKifuApp.Models;

namespace ShogiKifuApp.Data;

public class UserProfileDatabase
{
    private const string DbName = "user_profiles.db3";
    private SQLiteAsyncConnection? _conn;

    private async Task<SQLiteAsyncConnection> GetConnAsync()
    {
        if (_conn != null) return _conn;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, DbName);
        _conn = new SQLiteAsyncConnection(dbPath);
        await _conn.CreateTableAsync<UserProfile>();
        return _conn;
    }

    public async Task<List<UserProfile>> GetAllAsync()
    {
        var db = await GetConnAsync();
        return await db.Table<UserProfile>()
                       .OrderByDescending(x => x.IsActive)
                       .ThenBy(x => x.UserName)
                       .ToListAsync();
    }

    public async Task<UserProfile?> GetActiveUserAsync()
    {
        var db = await GetConnAsync();
        return await db.Table<UserProfile>()
                       .Where(x => x.IsActive)
                       .FirstOrDefaultAsync();
    }

    public async Task<int> InsertAsync(UserProfile profile)
    {
        var db = await GetConnAsync();
        return await db.InsertAsync(profile);
    }

    public async Task<int> UpdateAsync(UserProfile profile)
    {
        var db = await GetConnAsync();
        return await db.UpdateAsync(profile);
    }

    public async Task<int> DeleteAsync(UserProfile profile)
    {
        var db = await GetConnAsync();
        return await db.DeleteAsync(profile);
    }

    public async Task SetActiveUserAsync(int userId)
    {
        var db = await GetConnAsync();
        
        // 全ユーザーを非アクティブに
        await db.ExecuteAsync("UPDATE user_profiles SET IsActive = 0");
        
        // 指定ユーザーをアクティブに
        await db.ExecuteAsync("UPDATE user_profiles SET IsActive = 1 WHERE Id = ?", userId);
    }
}