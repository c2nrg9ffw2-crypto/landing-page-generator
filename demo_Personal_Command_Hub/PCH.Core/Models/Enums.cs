namespace PCH.Core.Models;

/// <summary>Lifecycle state of a task.</summary>
public enum TaskState
{
    Todo = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3
}

/// <summary>High-level grouping for a task on the dashboard.</summary>
public enum TaskCategory
{
    General = 0,
    Work = 1,
    School = 2,
    Personal = 3,
    Booking = 4,
    Finance = 5
}

/// <summary>Where a record originated, used to avoid duplicate ingestion.</summary>
public enum ItemSource
{
    Manual = 0,
    Email = 1,
    Booking = 2,
    Rss = 3
}

/// <summary>LLM-assigned category for an email that passed the keyword pre-filter.</summary>
public enum EmailType
{
    Other = 0,
    Booking = 1,
    Deadline = 2,
    Meeting = 3,
    Invoice = 4
}
