using AI_Readiness_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ClientCompany> ClientCompanies => Set<ClientCompany>();
    public DbSet<ClientWorkflowStep> ClientWorkflowSteps => Set<ClientWorkflowStep>();
    public DbSet<ReadinessFormSettings> ReadinessFormSettings => Set<ReadinessFormSettings>();
    public DbSet<ReadinessAssessment> ReadinessAssessments => Set<ReadinessAssessment>();
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

        modelBuilder.Entity<AIUseCaseScore>()
            .Property(score => score.PriorityScore)
            .HasPrecision(4, 2);

        modelBuilder.Entity<ClientCompany>()
            .HasIndex(client => client.CompanyName);

        modelBuilder.Entity<ReadinessFormSettings>()
            .HasIndex(settings => settings.IsActive);

        modelBuilder.Entity<ReadinessAssessment>()
            .HasIndex(assessment => assessment.ClientToken);

        modelBuilder.Entity<ReadinessAssessment>()
            .HasIndex(assessment => assessment.ExternalResponseId);

        modelBuilder.Entity<ClientCompany>()
            .HasMany(client => client.ReadinessAssessments)
            .WithOne(assessment => assessment.ClientCompany)
            .HasForeignKey(assessment => assessment.ClientCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReadinessAssessment>()
            .HasMany(assessment => assessment.Answers)
            .WithOne(answer => answer.ReadinessAssessment)
            .HasForeignKey(answer => answer.ReadinessAssessmentId)
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
    }
}
