using AI_Readiness_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<ClientCompany> ClientCompanies => Set<ClientCompany>();
    public DbSet<ClientWorkflowStep> ClientWorkflowSteps => Set<ClientWorkflowStep>();
    public DbSet<ReadinessFormSettings> ReadinessFormSettings => Set<ReadinessFormSettings>();
    public DbSet<ReadinessAssessment> ReadinessAssessments => Set<ReadinessAssessment>();
    public DbSet<AssessmentResponse> AssessmentResponses => Set<AssessmentResponse>();
    public DbSet<AssessmentAnswer> AssessmentAnswers => Set<AssessmentAnswer>();
    public DbSet<ClientDocument> ClientDocuments => Set<ClientDocument>();
    public DbSet<ConsultantNote> ConsultantNotes => Set<ConsultantNote>();
    public DbSet<MeetingTranscript> MeetingTranscripts => Set<MeetingTranscript>();
    public DbSet<AIAnalysisOutput> AIAnalysisOutputs => Set<AIAnalysisOutput>();
    public DbSet<GapAnalysisItem> GapAnalysisItems => Set<GapAnalysisItem>();
    public DbSet<SwotAnalysisItem> SwotAnalysisItems => Set<SwotAnalysisItem>();
    public DbSet<IndustryInsight> IndustryInsights => Set<IndustryInsight>();
    public DbSet<CompetitorInsight> CompetitorInsights => Set<CompetitorInsight>();
    public DbSet<AIUseCase> AIUseCases => Set<AIUseCase>();
    public DbSet<AIUseCaseScore> AIUseCaseScores => Set<AIUseCaseScore>();
    public DbSet<ReadinessScore> ReadinessScores => Set<ReadinessScore>();
    public DbSet<AIRoadmapItem> AIRoadmapItems => Set<AIRoadmapItem>();
    public DbSet<ClientReport> ClientReports => Set<ClientReport>();
    public DbSet<ReportSection> ReportSections => Set<ReportSection>();
    public DbSet<ClientTask> ClientTasks => Set<ClientTask>();
    public DbSet<ClientActivityLog> ClientActivityLogs => Set<ClientActivityLog>();
    public DbSet<KnowledgeGapItem> KnowledgeGapItems => Set<KnowledgeGapItem>();
    public DbSet<AIOutputSource> AIOutputSources => Set<AIOutputSource>();
    public DbSet<AIWorkspaceSession> AIWorkspaceSessions => Set<AIWorkspaceSession>();
    public DbSet<AIWorkspaceMessage> AIWorkspaceMessages => Set<AIWorkspaceMessage>();
    public DbSet<AIOutputRevision> AIOutputRevisions => Set<AIOutputRevision>();
    public DbSet<PromptDefinition> PromptDefinitions => Set<PromptDefinition>();
    public DbSet<ReportTemplateSection> ReportTemplateSections => Set<ReportTemplateSection>();
    public DbSet<UseCaseLibraryItem> UseCaseLibraryItems => Set<UseCaseLibraryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.ClrType.GetProperties())
            {
                var enumType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (enumType.IsEnum)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasConversion<string>()
                        .HasMaxLength(80);
                }
            }
        }

        modelBuilder.Entity<ClientCompany>()
            .Property(client => client.OverallReadinessScore)
            .HasPrecision(5, 2);

        modelBuilder.Entity<DataProtectionKey>()
            .HasIndex(key => key.FriendlyName)
            .IsUnique();

        modelBuilder.Entity<AIUseCaseScore>()
            .Property(score => score.PriorityScore)
            .HasPrecision(4, 2);

        modelBuilder.Entity<ClientCompany>()
            .HasIndex(client => client.CompanyName);

        modelBuilder.Entity<ClientCompany>()
            .HasIndex(client => client.CurrentStage);

        modelBuilder.Entity<ClientCompany>()
            .HasIndex(client => client.LastModifiedAt);

        modelBuilder.Entity<ReadinessFormSettings>()
            .HasIndex(settings => settings.IsActive);

        modelBuilder.Entity<ReadinessAssessment>()
            .HasIndex(assessment => assessment.ClientToken);

        modelBuilder.Entity<ReadinessAssessment>()
            .HasIndex(assessment => new { assessment.ClientCompanyId, assessment.CreatedAt });

        modelBuilder.Entity<ReadinessAssessment>()
            .HasIndex(assessment => new { assessment.FormStatus, assessment.SentAt });

        modelBuilder.Entity<ReadinessAssessment>()
            .HasIndex(assessment => assessment.ExternalResponseId);

        modelBuilder.Entity<AssessmentResponse>()
            .HasIndex(response => new { response.ReadinessAssessmentId, response.ResponseNumber })
            .IsUnique();

        modelBuilder.Entity<AssessmentResponse>()
            .HasIndex(response => new { response.ReadinessAssessmentId, response.ExternalResponseId });

        modelBuilder.Entity<AssessmentResponse>()
            .HasIndex(response => new { response.ReadinessAssessmentId, response.ReceivedAt });

        modelBuilder.Entity<AssessmentResponse>()
            .HasIndex(response => response.ReceivedAt);

        modelBuilder.Entity<AssessmentResponse>()
            .HasIndex(response => response.ExternalResponseId);

        modelBuilder.Entity<AssessmentResponse>()
            .HasIndex(response => new { response.Status, response.ReceivedAt });

        modelBuilder.Entity<AssessmentAnswer>()
            .HasIndex(answer => answer.AssessmentResponseId);

        modelBuilder.Entity<ClientWorkflowStep>()
            .HasIndex(step => new { step.ClientCompanyId, step.DisplayOrder });

        modelBuilder.Entity<ClientDocument>()
            .HasIndex(document => new { document.ClientCompanyId, document.UploadedAt });

        modelBuilder.Entity<ConsultantNote>()
            .HasIndex(note => new { note.ClientCompanyId, note.CreatedAt });

        modelBuilder.Entity<MeetingTranscript>()
            .HasIndex(transcript => new { transcript.ClientCompanyId, transcript.SessionDate });

        modelBuilder.Entity<AIAnalysisOutput>()
            .HasIndex(output => new { output.ClientCompanyId, output.AnalysisType, output.VersionNumber });

        modelBuilder.Entity<GapAnalysisItem>()
            .HasIndex(gap => new { gap.ClientCompanyId, gap.Status });

        modelBuilder.Entity<AIUseCase>()
            .HasIndex(useCase => useCase.ClientCompanyId);

        modelBuilder.Entity<ClientReport>()
            .HasIndex(report => new { report.ClientCompanyId, report.VersionNumber });

        modelBuilder.Entity<ClientReport>()
            .HasIndex(report => new { report.ClientCompanyId, report.ReportStatus });

        modelBuilder.Entity<ClientReport>()
            .HasIndex(report => new { report.ReportStatus, report.GeneratedAt });

        modelBuilder.Entity<ClientTask>()
            .HasIndex(task => new { task.ClientCompanyId, task.Status });

        modelBuilder.Entity<ClientTask>()
            .HasIndex(task => new { task.Status, task.DueDate });

        modelBuilder.Entity<ClientActivityLog>()
            .HasIndex(activity => new { activity.ClientCompanyId, activity.CreatedAt });

        modelBuilder.Entity<ReadinessScore>()
            .HasIndex(score => new { score.ClientCompanyId, score.CreatedAt });

        modelBuilder.Entity<KnowledgeGapItem>()
            .HasIndex(item => new { item.ClientCompanyId, item.Status });

        modelBuilder.Entity<KnowledgeGapItem>()
            .HasIndex(item => new { item.ClientCompanyId, item.Priority });

        modelBuilder.Entity<KnowledgeGapItem>()
            .HasIndex(item => item.AssessmentResponseId);

        modelBuilder.Entity<AIOutputSource>()
            .HasIndex(source => new { source.ClientCompanyId, source.OutputType, source.OutputId });

        modelBuilder.Entity<AIWorkspaceSession>()
            .HasIndex(session => new { session.ClientCompanyId, session.OutputType, session.OutputId, session.Status });

        modelBuilder.Entity<AIWorkspaceMessage>()
            .HasIndex(message => new { message.AIWorkspaceSessionId, message.CreatedAt });

        modelBuilder.Entity<AIOutputRevision>()
            .HasIndex(revision => new { revision.ClientCompanyId, revision.OutputType, revision.OutputId, revision.VersionNumber });

        modelBuilder.Entity<PromptDefinition>()
            .HasIndex(prompt => new { prompt.PromptName, prompt.VersionNumber })
            .IsUnique();

        modelBuilder.Entity<ReportTemplateSection>()
            .HasIndex(section => new { section.SectionOrder, section.SectionTitle })
            .IsUnique();

        modelBuilder.Entity<UseCaseLibraryItem>()
            .HasIndex(item => item.Name)
            .IsUnique();

        modelBuilder.Entity<ClientCompany>()
            .HasMany(client => client.ReadinessAssessments)
            .WithOne(assessment => assessment.ClientCompany)
            .HasForeignKey(assessment => assessment.ClientCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReadinessAssessment>()
            .HasMany(assessment => assessment.Responses)
            .WithOne(response => response.ReadinessAssessment)
            .HasForeignKey(response => response.ReadinessAssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReadinessAssessment>()
            .HasMany(assessment => assessment.Answers)
            .WithOne(answer => answer.ReadinessAssessment)
            .HasForeignKey(answer => answer.ReadinessAssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssessmentResponse>()
            .HasMany(response => response.Answers)
            .WithOne(answer => answer.AssessmentResponse)
            .HasForeignKey(answer => answer.AssessmentResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AIUseCase>()
            .HasOne(useCase => useCase.Score)
            .WithOne(score => score.AIUseCase)
            .HasForeignKey<AIUseCaseScore>(score => score.AIUseCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AIRoadmapItem>()
            .HasOne(item => item.RelatedUseCase)
            .WithMany()
            .HasForeignKey(item => item.RelatedUseCaseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ClientReport>()
            .HasMany(report => report.Sections)
            .WithOne(section => section.ClientReport)
            .HasForeignKey(section => section.ClientReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClientCompany>()
            .HasMany(client => client.KnowledgeGapItems)
            .WithOne(item => item.ClientCompany)
            .HasForeignKey(item => item.ClientCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KnowledgeGapItem>()
            .HasOne(item => item.AssessmentResponse)
            .WithMany()
            .HasForeignKey(item => item.AssessmentResponseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ConsultantNote>()
            .HasOne(note => note.KnowledgeGapItem)
            .WithMany()
            .HasForeignKey(note => note.KnowledgeGapItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MeetingTranscript>()
            .HasOne(transcript => transcript.KnowledgeGapItem)
            .WithMany()
            .HasForeignKey(transcript => transcript.KnowledgeGapItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ClientCompany>()
            .HasMany(client => client.AIOutputSources)
            .WithOne(source => source.ClientCompany)
            .HasForeignKey(source => source.ClientCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClientCompany>()
            .HasMany(client => client.AIWorkspaceSessions)
            .WithOne(session => session.ClientCompany)
            .HasForeignKey(session => session.ClientCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AIWorkspaceSession>()
            .HasMany(session => session.Messages)
            .WithOne(message => message.AIWorkspaceSession)
            .HasForeignKey(message => message.AIWorkspaceSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AIWorkspaceSession>()
            .HasMany(session => session.Revisions)
            .WithOne(revision => revision.AIWorkspaceSession)
            .HasForeignKey(revision => revision.AIWorkspaceSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ClientCompany>()
            .HasMany(client => client.AIOutputRevisions)
            .WithOne(revision => revision.ClientCompany)
            .HasForeignKey(revision => revision.ClientCompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
