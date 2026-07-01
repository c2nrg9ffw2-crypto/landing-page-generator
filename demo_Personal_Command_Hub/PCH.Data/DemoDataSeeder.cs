using PCH.Core.Models;

namespace PCH.Data;

/// <summary>
/// Seeds obviously fake demo data on first run so the dashboard is never empty.
/// Demo data (tasks/emails/news) is only inserted when all three tables are empty.
/// RSS feeds and app settings are seeded independently on every startup if missing.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(PchDbContext db)
    {
        bool hasDemo = db.Tasks.Any() || db.Emails.Any() || db.NewsItems.Any();
        if (!hasDemo)
        {
            var now = DateTimeOffset.UtcNow;

            // ── Tasks ────────────────────────────────────────────────────────────
            db.Tasks.AddRange(
                new TaskItem
                {
                    Title       = "Build demo dashboard",
                    Description = "Set up the Personal Command Hub demo environment with fake data.",
                    Category    = TaskCategory.Work,
                    Status      = TaskState.Done,
                    Progress    = 100,
                    Source      = ItemSource.Manual,
                    CreatedAt   = now.AddDays(-5),
                    UpdatedAt   = now.AddDays(-1)
                },
                new TaskItem
                {
                    Title       = "Review Q3 budget report",
                    Description = "Go through the fictional quarterly numbers and flag any anomalies.",
                    Category    = TaskCategory.Finance,
                    Status      = TaskState.InProgress,
                    Progress    = 65,
                    DueDate     = now.AddDays(1),
                    Source      = ItemSource.Manual,
                    CreatedAt   = now.AddDays(-3),
                    UpdatedAt   = now.AddDays(-1)
                },
                new TaskItem
                {
                    Title       = "Book dentist appointment",
                    Description = "Call Fake Dental Clinic at 555-0100 to schedule a checkup.",
                    Category    = TaskCategory.Personal,
                    Status      = TaskState.Todo,
                    Progress    = 0,
                    DueDate     = now.AddDays(7),
                    Source      = ItemSource.Manual,
                    CreatedAt   = now.AddDays(-2),
                    UpdatedAt   = now.AddDays(-2)
                },
                new TaskItem
                {
                    Title       = "Submit assignment: Data Structures",
                    Description = "Complete exercises 4–7 in chapter 12 and upload to FakeLearn portal.",
                    Category    = TaskCategory.School,
                    Status      = TaskState.InProgress,
                    Progress    = 40,
                    DueDate     = now.AddDays(2),
                    Source      = ItemSource.Email,
                    ExternalId  = "demo-email-4",
                    CreatedAt   = now.AddDays(-1),
                    UpdatedAt   = now.AddDays(-1)
                },
                new TaskItem
                {
                    Title       = "Prepare Q3 presentation slides",
                    Description = "10-slide deck for the fictional all-hands meeting on Friday.",
                    Category    = TaskCategory.Work,
                    Status      = TaskState.InProgress,
                    Progress    = 75,
                    DueDate     = now.AddDays(3),
                    Source      = ItemSource.Manual,
                    CreatedAt   = now.AddDays(-4),
                    UpdatedAt   = now.AddHours(-6)
                },
                new TaskItem
                {
                    Title       = "Call landlord about heating",
                    Description = "Radiator in bedroom has been making a clanking noise since Monday.",
                    Category    = TaskCategory.Personal,
                    Status      = TaskState.Todo,
                    Progress    = 0,
                    Source      = ItemSource.Manual,
                    CreatedAt   = now.AddDays(-1),
                    UpdatedAt   = now.AddDays(-1)
                }
            );

            // ── Emails ───────────────────────────────────────────────────────────
            db.Emails.AddRange(
                new Email
                {
                    MessageId       = "demo-msg-1@fakebank.example",
                    Subject         = "Your invoice #INV-2026-0042 is ready",
                    Sender          = "billing@fakebank.example",
                    ReceivedAt      = now.AddHours(-3),
                    BodyPreview     = "Dear Demo User, your invoice #INV-2026-0042 for the amount of $149.99 " +
                                      "is now available in your account portal at fakebank.example/invoices. " +
                                      "Payment is due within 30 days. This is a fake email for demo purposes.",
                    IsKeywordMatch  = true,
                    EmailType       = EmailType.Invoice,
                    LlmSummary      = "Invoice #INV-2026-0042 for $149.99 is ready. Payment due in 30 days.",
                    FetchedAt       = now.AddHours(-3)
                },
                new Email
                {
                    MessageId       = "demo-msg-2@fakedental.example",
                    Subject         = "Appointment confirmation: 15 Jul at 10:00",
                    Sender          = "bookings@fakedental.example",
                    ReceivedAt      = now.AddDays(-1),
                    BodyPreview     = "Hi Demo User! This confirms your appointment at Fake Dental Clinic " +
                                      "on Tuesday 15 July 2026 at 10:00 AM with Dr. Alice Placeholder. " +
                                      "Please arrive 10 minutes early. This is a fake email for demo purposes.",
                    IsKeywordMatch  = true,
                    EmailType       = EmailType.Booking,
                    LlmSummary      = "Dentist appointment confirmed at Fake Dental Clinic on 15 Jul 2026 at 10:00.",
                    FetchedAt       = now.AddDays(-1)
                },
                new Email
                {
                    MessageId       = "demo-msg-3@fakecorp.example",
                    Subject         = "Team meeting rescheduled to Thursday 14:00",
                    Sender          = "hr@fakecorp.example",
                    ReceivedAt      = now.AddHours(-8),
                    BodyPreview     = "Hello team, the weekly sync has been moved from Wednesday to Thursday " +
                                      "at 14:00 (CET) due to a scheduling conflict. Meeting link: " +
                                      "https://meet.fakecorp.example/weekly-sync. This is a fake email for demo purposes.",
                    IsKeywordMatch  = true,
                    EmailType       = EmailType.Meeting,
                    LlmSummary      = "Weekly team sync moved to Thursday at 14:00 CET. Same meeting link.",
                    FetchedAt       = now.AddHours(-8)
                },
                new Email
                {
                    MessageId       = "demo-email-4@fakeschool.example",
                    Subject         = "Reminder: Data Structures assignment due in 48 h",
                    Sender          = "noreply@fakeschool.example",
                    ReceivedAt      = now.AddHours(-12),
                    BodyPreview     = "Dear Student, this is a reminder that your Data Structures assignment " +
                                      "(exercises 4–7, chapter 12) is due in 48 hours. Submit via the FakeLearn " +
                                      "portal before the deadline. This is a fake email for demo purposes.",
                    IsKeywordMatch  = true,
                    EmailType       = EmailType.Deadline,
                    LlmSummary      = "Data Structures assignment (ch.12 ex.4–7) due in 48 h — submit on FakeLearn.",
                    LinkedTaskId    = null,
                    FetchedAt       = now.AddHours(-12)
                },
                new Email
                {
                    MessageId       = "demo-msg-5@newsletter.example",
                    Subject         = "Weekly Digest: Top Stories in Tech",
                    Sender          = "digest@newsletter.example",
                    ReceivedAt      = now.AddDays(-2),
                    BodyPreview     = "This week in fake tech news: AI adoption up 40%, open-source hits a " +
                                      "milestone, and a new programming language promises to replace everything. " +
                                      "This is a fake newsletter email for demo purposes.",
                    IsKeywordMatch  = false,
                    EmailType       = EmailType.Other,
                    FetchedAt       = now.AddDays(-2)
                }
            );

            // ── News ─────────────────────────────────────────────────────────────
            db.NewsItems.AddRange(
                new NewsItem
                {
                    FeedCategory = "Sweden",
                    Title        = "Stockholm City Council Approves New Transit Expansion Plan",
                    Link         = "https://fake-news.example/sweden/stockholm-transit-2026",
                    Summary      = "The council voted 45–12 in favour of extending the metro " +
                                   "to three new districts by 2031. This is a fake news item for demo purposes.",
                    Published    = now.AddHours(-2),
                    FetchedAt    = now.AddHours(-2)
                },
                new NewsItem
                {
                    FeedCategory = "Sweden",
                    Title        = "Swedish Tech Startup FakeAI Raises €50 M in Series B",
                    Link         = "https://fake-news.example/sweden/fakeai-series-b",
                    Summary      = "Gothenburg-based FakeAI secured €50 million to expand its " +
                                   "synthetic data platform across Europe. This is a fake news item for demo purposes.",
                    Published    = now.AddHours(-5),
                    FetchedAt    = now.AddHours(-5)
                },
                new NewsItem
                {
                    FeedCategory = "Tech",
                    Title        = "AI Assistants Now Power 40 % of Customer Support Calls, Study Finds",
                    Link         = "https://fake-news.example/tech/ai-customer-support-study",
                    Summary      = "A fictional industry report shows AI-driven support reduced " +
                                   "average resolution time by 35 %. This is a fake news item for demo purposes.",
                    Published    = now.AddHours(-7),
                    FetchedAt    = now.AddHours(-7)
                },
                new NewsItem
                {
                    FeedCategory = "Tech",
                    Title        = "Open-Source Framework 'FakeKit' Hits 100 000 GitHub Stars",
                    Link         = "https://fake-news.example/tech/fakekit-100k-stars",
                    Summary      = "The lightweight UI toolkit celebrated the milestone with a " +
                                   "v3.0 release featuring SSR support. This is a fake news item for demo purposes.",
                    Published    = now.AddDays(-1),
                    FetchedAt    = now.AddDays(-1)
                },
                new NewsItem
                {
                    FeedCategory = "World",
                    Title        = "Global Leaders Convene at Fictional Climate Summit in Geneva",
                    Link         = "https://fake-news.example/world/fictional-climate-summit-2026",
                    Summary      = "Representatives from 140 imaginary nations signed the " +
                                   "Non-Binding Declaration on Carbon Pretending. This is a fake news item for demo purposes.",
                    Published    = now.AddHours(-10),
                    FetchedAt    = now.AddHours(-10)
                },
                new NewsItem
                {
                    FeedCategory = "Science",
                    Title        = "Researchers Discover New Deep-Sea Fish Species off Fake Island",
                    Link         = "https://fake-news.example/science/deep-sea-fish-fake-island",
                    Summary      = "The bioluminescent Pseudolampris fictus was found at 4 000 m " +
                                   "depth during the FakeOcean 2026 expedition. This is a fake news item for demo purposes.",
                    Published    = now.AddDays(-2),
                    FetchedAt    = now.AddDays(-2)
                }
            );

            await db.SaveChangesAsync();
        }

        // ── RSS Feeds (seeded independently — safe to call on every startup) ────
        if (!db.RssFeeds.Any())
        {
            db.RssFeeds.AddRange(
                new RssFeed { Url = "https://www.svt.se/nyheter/rss.xml",               Category = "Sweden",  Enabled = true },
                new RssFeed { Url = "https://www.tagesschau.de/xml/rss2/",              Category = "Germany", Enabled = true },
                new RssFeed { Url = "https://www.sciencedaily.com/rss/top/science.xml", Category = "Science", Enabled = true }
            );
            await db.SaveChangesAsync();
        }

        // ── App Settings (seeded independently — safe to call on every startup) ─
        if (!db.AppSettings.Any())
        {
            db.AppSettings.Add(new AppSettings
            {
                Id                     = 1,
                ImapHost               = "imap.outlook.com",
                ImapPort               = 993,
                ImapSsl                = true,
                NotifyNewTask          = true,
                NotifyDeadlineToday    = true,
                NotifyDailyNewsSummary = true,
                NewsSummaryHour        = 8
            });
            await db.SaveChangesAsync();
        }
    }
}
