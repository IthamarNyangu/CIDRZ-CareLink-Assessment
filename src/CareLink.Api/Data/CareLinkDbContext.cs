using CareLink.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Api.Data;

public sealed class CareLinkDbContext(DbContextOptions<CareLinkDbContext> options)
    : DbContext(options)
{
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Visit> Visits => Set<Visit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var facility = modelBuilder.Entity<Facility>();
        facility.ToTable("facility");
        facility.HasKey(x => x.Id);
        facility.Property(x => x.Id).HasColumnName("facility_id").HasMaxLength(20);
        facility.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        facility.Property(x => x.District).HasColumnName("district").HasMaxLength(100).IsRequired();

        var patient = modelBuilder.Entity<Patient>();
        patient.ToTable("patient");
        patient.HasKey(x => x.Id);
        patient.Property(x => x.Id).HasColumnName("id");
        patient.Property(x => x.FacilityId).HasColumnName("facility_id").HasMaxLength(20);
        patient.Property(x => x.PatientNumber).HasColumnName("patient_number").HasMaxLength(40);
        patient.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100);
        patient.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100);
        patient.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
        patient.Property(x => x.Sex).HasColumnName("sex").HasConversion<string>().HasMaxLength(10);
        patient.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30);
        patient.Property(x => x.CreatedAt).HasColumnName("created_at");
        patient.HasIndex(x => new { x.FacilityId, x.PatientNumber }).IsUnique();
        patient.HasIndex(x => x.FacilityId);
        patient.HasOne(x => x.Facility)
            .WithMany(x => x.Patients)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        var visit = modelBuilder.Entity<Visit>();
        visit.ToTable("visit", table =>
            table.HasCheckConstraint(
                "CK_visit_next_appointment",
                "next_appointment_date IS NULL OR next_appointment_date >= visit_date"));
        visit.HasKey(x => x.Id);
        visit.Property(x => x.Id).HasColumnName("id");
        visit.Property(x => x.PatientId).HasColumnName("patient_id");
        visit.Property(x => x.VisitDate).HasColumnName("visit_date");
        visit.Property(x => x.NextAppointmentDate).HasColumnName("next_appointment_date");
        visit.Property(x => x.VisitType).HasColumnName("visit_type").HasConversion<string>().HasMaxLength(20);
        visit.Property(x => x.CreatedAt).HasColumnName("created_at");
        visit.HasIndex(x => new { x.PatientId, x.VisitDate });
        visit.HasOne(x => x.Patient)
            .WithMany(x => x.Visits)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
