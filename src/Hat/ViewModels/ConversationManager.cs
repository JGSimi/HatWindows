using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Hat.Models;

namespace Hat.ViewModels;

/// <summary>
/// Manages conversation CRUD, persistence, and grouping.
/// Port of ConversationManager.swift.
/// Stores conversations as JSON in %APPDATA%/Hat/conversations/.
/// </summary>
public partial class ConversationManager : ObservableObject
{
    private const int MaxConversations = 50;
    private const int MaxMessagesPerConversation = 200;

    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [ObservableProperty]
    private ObservableCollection<Conversation> _conversations = new();

    [ObservableProperty]
    private Guid? _activeConversationId;

    public Conversation? ActiveConversation =>
        ActiveConversationId.HasValue
            ? Conversations.FirstOrDefault(c => c.Id == ActiveConversationId.Value)
            : null;

    public ConversationManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storageDirectory = Path.Combine(appData, "Hat", "conversations");
        Directory.CreateDirectory(_storageDirectory);
        LoadConversations();
        PruneOldConversations();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - CRUD
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public Conversation CreateConversation()
    {
        var conversation = new Conversation();
        Conversations.Insert(0, conversation);
        ActiveConversationId = conversation.Id;
        SaveIndex();
        PruneOldConversations();
        return conversation;
    }

    public void SelectConversation(Guid id)
    {
        ActiveConversationId = id;
        OnPropertyChanged(nameof(ActiveConversation));
    }

    public void DeleteConversation(Guid id)
    {
        var conv = Conversations.FirstOrDefault(c => c.Id == id);
        if (conv != null)
            Conversations.Remove(conv);

        // Delete file
        var filePath = Path.Combine(_storageDirectory, $"{id}.json");
        try { File.Delete(filePath); } catch { }

        if (ActiveConversationId == id)
        {
            ActiveConversationId = Conversations.FirstOrDefault()?.Id;
            OnPropertyChanged(nameof(ActiveConversation));
        }
        SaveIndex();
    }

    public void RenameConversation(Guid id, string title)
    {
        var conv = Conversations.FirstOrDefault(c => c.Id == id);
        if (conv == null) return;
        conv.Title = title;
        SaveConversation(conv);
        SaveIndex();
    }

    public void TogglePin(Guid id)
    {
        var conv = Conversations.FirstOrDefault(c => c.Id == id);
        if (conv == null) return;
        conv.IsPinned = !conv.IsPinned;
        SaveConversation(conv);
        SaveIndex();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Messages
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void AddMessage(string content, bool isUser, MessageSource source = MessageSource.Chat)
    {
        // Create conversation if none active
        if (!ActiveConversationId.HasValue)
            CreateConversation();

        var conv = Conversations.FirstOrDefault(c => c.Id == ActiveConversationId);
        if (conv == null) return;

        var savedMessage = new SavedMessage
        {
            Content = content,
            IsUser = isUser,
            Timestamp = DateTime.Now,
            Source = source == MessageSource.ScreenAnalysis ? "screenAnalysis" : "chat"
        };

        conv.Messages.Add(savedMessage);
        conv.UpdatedAt = DateTime.Now;

        // Auto-title from first user message
        if (isUser && conv.Messages.Count(m => m.IsUser) == 1)
            conv.AutoTitle();

        // Trim messages if over limit
        if (conv.Messages.Count > MaxMessagesPerConversation)
        {
            conv.Messages = conv.Messages
                .Skip(conv.Messages.Count - MaxMessagesPerConversation)
                .ToList();
        }

        // Move to top of list (most recent)
        var index = Conversations.IndexOf(conv);
        if (index > 0)
        {
            Conversations.Move(index, 0);
        }

        SaveConversation(conv);
        SaveIndex();
    }

    public void ClearActiveConversation()
    {
        var conv = ActiveConversation;
        if (conv == null) return;

        conv.Messages.Clear();
        conv.UpdatedAt = DateTime.Now;
        SaveConversation(conv);
        SaveIndex();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Grouping
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public List<(ConversationGroup Group, List<Conversation> Items)> GetGroupedConversations()
    {
        var now = DateTime.Now;
        var startOfToday = now.Date;
        var startOfYesterday = startOfToday.AddDays(-1);
        var startOfLastWeek = startOfToday.AddDays(-7);

        var pinned = new List<Conversation>();
        var today = new List<Conversation>();
        var yesterday = new List<Conversation>();
        var lastWeek = new List<Conversation>();
        var older = new List<Conversation>();

        foreach (var conv in Conversations)
        {
            if (conv.IsPinned) pinned.Add(conv);
            else if (conv.UpdatedAt >= startOfToday) today.Add(conv);
            else if (conv.UpdatedAt >= startOfYesterday) yesterday.Add(conv);
            else if (conv.UpdatedAt >= startOfLastWeek) lastWeek.Add(conv);
            else older.Add(conv);
        }

        var result = new List<(ConversationGroup, List<Conversation>)>();
        if (pinned.Count > 0) result.Add((ConversationGroup.Pinned, pinned));
        if (today.Count > 0) result.Add((ConversationGroup.Today, today));
        if (yesterday.Count > 0) result.Add((ConversationGroup.Yesterday, yesterday));
        if (lastWeek.Count > 0) result.Add((ConversationGroup.LastWeek, lastWeek));
        if (older.Count > 0) result.Add((ConversationGroup.Older, older));

        return result;
    }

    /// <summary>
    /// Searches conversations by title or preview text.
    /// </summary>
    public List<Conversation> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Conversations.ToList();

        var lower = query.ToLowerInvariant();
        return Conversations
            .Where(c => c.Title.Contains(lower, StringComparison.InvariantCultureIgnoreCase) ||
                        c.Preview.Contains(lower, StringComparison.InvariantCultureIgnoreCase))
            .ToList();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Persistence
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void SaveConversation(Conversation conversation)
    {
        var filePath = Path.Combine(_storageDirectory, $"{conversation.Id}.json");
        Task.Run(() =>
        {
            try
            {
                var json = JsonSerializer.Serialize(conversation, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save conversation: {ex.Message}");
            }
        });
    }

    private Conversation? LoadConversation(Guid id)
    {
        var filePath = Path.Combine(_storageDirectory, $"{id}.json");
        try
        {
            if (!File.Exists(filePath)) return null;
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Conversation>(json, _jsonOptions);
        }
        catch { return null; }
    }

    private void SaveIndex()
    {
        var entries = Conversations.Select(c => new ConversationIndex
        {
            Id = c.Id,
            Title = c.Title,
            IsPinned = c.IsPinned,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        var indexPath = Path.Combine(_storageDirectory, "index.json");
        Task.Run(() =>
        {
            try
            {
                var json = JsonSerializer.Serialize(entries, _jsonOptions);
                File.WriteAllText(indexPath, json);
            }
            catch { }
        });
    }

    private void LoadConversations()
    {
        var indexPath = Path.Combine(_storageDirectory, "index.json");
        try
        {
            if (!File.Exists(indexPath)) return;
            var json = File.ReadAllText(indexPath);
            var entries = JsonSerializer.Deserialize<List<ConversationIndex>>(json, _jsonOptions);
            if (entries == null) return;

            foreach (var entry in entries)
            {
                var conv = LoadConversation(entry.Id);
                if (conv != null)
                    Conversations.Add(conv);
            }
        }
        catch { }
    }

    private void PruneOldConversations()
    {
        var unpinned = Conversations.Where(c => !c.IsPinned).ToList();
        if (unpinned.Count <= MaxConversations) return;

        var toRemove = unpinned.Skip(MaxConversations).ToList();
        foreach (var conv in toRemove)
            DeleteConversation(conv.Id);
    }
}
