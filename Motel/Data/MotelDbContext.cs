using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Motel.Models;

namespace Motel.Data;

public partial class MotelDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public MotelDbContext(DbContextOptions<MotelDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApplicationUser> AspNetUsers { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceLine> InvoiceLines { get; set; }

    public virtual DbSet<Landlord> Landlords { get; set; }

    public virtual DbSet<MeterReading> MeterReadings { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentIntent> PaymentIntents { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomOccupancy> RoomOccupancies { get; set; }

    public virtual DbSet<RoomUtilitySetting> RoomUtilitySettings { get; set; }

    public virtual DbSet<StoredFile> StoredFiles { get; set; }

    public virtual DbSet<StoredFileReference> StoredFileReferences { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.Email, "UX_AspNetUsers_Email").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(30);
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasIndex(e => e.RoomId, "IX_Contracts_RoomId").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.RoomId, "UX_Contracts_RoomId_ActiveOnly")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0) AND [Status]='active')");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Room).WithOne(p => p.Contract)
                .HasForeignKey<Contract>(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contracts_Rooms");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contracts_Tenants");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(e => e.ContractId, "IX_Invoices_ContractId");

            entity.HasIndex(e => new { e.RoomId, e.PeriodMonth }, "UX_Invoices_RoomId_PeriodMonth").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Contract).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Contracts");

            entity.HasOne(d => d.Room).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Rooms");
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.ItemType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("(round([Quantity]*[UnitPrice],(2)))", true)
                .HasColumnType("decimal(37, 4)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceLines)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceLines_Invoices");
        });

        modelBuilder.Entity<Landlord>(entity =>
        {
            entity.HasIndex(e => e.UserId, "UX_Landlords_UserId_Active")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DisplayName).HasMaxLength(150);

            entity.HasOne(d => d.User).WithOne(p => p.Landlord)
                .HasForeignKey<Landlord>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Landlords_AspNetUsers");
        });

        modelBuilder.Entity<MeterReading>(entity =>
        {
            entity.HasIndex(e => new { e.RoomId, e.PeriodMonth }, "UX_MeterReadings_RoomId_PeriodMonth").IsUnique();

            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.RecordedByUser).WithMany(p => p.MeterReadings)
                .HasForeignKey(d => d.RecordedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MeterReadings_AspNetUsers");

            entity.HasOne(d => d.Room).WithMany(p => p.MeterReadings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MeterReadings_Rooms");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.Channel)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(150);
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Landlord).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.LandlordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_Landlords");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.TenantId)
                .HasConstraintName("FK_Notifications_Tenants");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.InvoiceId, "IX_Payments_InvoiceId");

            entity.HasIndex(e => e.ProviderTxnId, "UX_Payments_ProviderTxnId").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaidAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Provider)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ProviderTxnId).HasMaxLength(120);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Invoices");

            entity.HasOne(d => d.PaymentIntent).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentIntentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_PaymentIntents");
        });

        modelBuilder.Entity<PaymentIntent>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Provider)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ProviderIntentId).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Invoice).WithMany(p => p.PaymentIntents)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentIntents_Invoices");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasIndex(e => new { e.LandlordId, e.Name }, "UX_Properties_LandlordId_Name_Active")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Name).HasMaxLength(150);

            entity.HasOne(d => d.Landlord).WithMany(p => p.Properties)
                .HasForeignKey(d => d.LandlordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Properties_Landlords");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_Rooms_BlockSoftDeleteWhenActiveContract"));

            entity.HasIndex(e => e.PropertyId, "IX_Rooms_PropertyId").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => new { e.PropertyId, e.RoomName }, "UX_Rooms_PropertyId_RoomName_Active")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.RentPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RoomName).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Property).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_Properties");
        });

        modelBuilder.Entity<RoomOccupancy>(entity =>
        {
            entity.HasKey(e => e.OccupancyId);

            entity.HasIndex(e => e.RoomId, "UX_RoomOccupancies_PrimaryActivePerRoom")
                .IsUnique()
                .HasFilter("([Status]='active' AND [IsPrimary]=(1))");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Room).WithOne(p => p.RoomOccupancy)
                .HasForeignKey<RoomOccupancy>(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoomOccupancies_Rooms");

            entity.HasOne(d => d.Tenant).WithMany(p => p.RoomOccupancies)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoomOccupancies_Tenants");
        });

        modelBuilder.Entity<RoomUtilitySetting>(entity =>
        {
            entity.HasKey(e => e.UtilitySettingId);

            entity.HasIndex(e => new { e.RoomId, e.EffectiveFrom }, "UX_RoomUtilitySettings_RoomId_EffectiveFrom").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ElectricUnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InternetFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrashFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.WaterUnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Room).WithMany(p => p.RoomUtilitySettings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoomUtilitySettings_Rooms");
        });

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("StoredFile");

            entity.HasIndex(e => e.StoragePath, "UX_StoredFile_StoragePath").IsUnique();

            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.MimeType).HasMaxLength(100);
            entity.Property(e => e.StoragePath).HasMaxLength(500);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Landlord).WithMany(p => p.StoredFiles)
                .HasForeignKey(d => d.LandlordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StoredFile_Landlords");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.StoredFiles)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StoredFile_AspNetUsers");
        });

        modelBuilder.Entity<StoredFileReference>(entity =>
        {
            entity.HasKey(e => e.StoredFileRefId);

            entity.ToTable("StoredFileReference");

            entity.HasIndex(e => new { e.StoredFileId, e.RefType, e.RefId }, "UX_StoredFileReference_Dedupe").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.RefType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.StoredFile).WithMany(p => p.StoredFileReferences)
                .HasForeignKey(d => d.StoredFileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StoredFileReference_StoredFile");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PlanName).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Landlord).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.LandlordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscriptions_Landlords");
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.LandlordId, "IX_Tenants_LandlordId").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => new { e.LandlordId, e.IdentityNo }, "UX_Tenants_LandlordId_IdentityNo_Active")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0) AND [IdentityNo] IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IdentityNo).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(30);

            entity.HasOne(d => d.Landlord).WithMany(p => p.Tenants)
                .HasForeignKey(d => d.LandlordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tenants_Landlords");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Direction)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Contract).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("FK_Transactions_Contracts");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transactions_AspNetUsers");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_Transactions_Invoices");

            entity.HasOne(d => d.Landlord).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.LandlordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transactions_Landlords");

            entity.HasOne(d => d.Room).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK_Transactions_Rooms");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.TenantId)
                .HasConstraintName("FK_Transactions_Tenants");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
