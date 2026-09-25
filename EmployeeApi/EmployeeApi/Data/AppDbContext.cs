using EmployeeApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EmployeeApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MeetingRoom> MeetingRooms => Set<MeetingRoom>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingParticipant> BookingParticipants => Set<BookingParticipant>();
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateOnly>()
            .HaveConversion<DateOnlyConverter>()
            .HaveColumnType("date");

        configurationBuilder.Properties<TimeOnly>()
            .HaveConversion<TimeOnlyConverter>()
            .HaveColumnType("time");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MeetingRoom 

        var equipmentComparer = new ValueComparer<List<string>>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            v => v.ToList());

        modelBuilder.Entity<MeetingRoom>(e =>
        {
            e.Property(r => r.Name).HasMaxLength(100).IsRequired();
            e.Property(r => r.Location).HasMaxLength(100).IsRequired();

            e.Property(r => r.Equipment)
                .HasConversion(
                    v => string.Join(';', v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    equipmentComparer)
                .HasMaxLength(500);

            e.HasData(
                new MeetingRoom { Id = 1, Name = "შეხვედრის ოთახი ცენტრალური", Location = "IV სართული", Capacity = 20,
                    Equipment = ["პროექტორი", "ეკრანი", "კონფერენც ტელეფონი", "თეთრი დაფა", "Wi-Fi"] },
                new MeetingRoom { Id = 2, Name = "შეხვედრის ოთახი სატვირთო", Location = "III სართული", Capacity = 12,
                    Equipment = ["TV ეკრანი", "ფლიპჩარტი", "Wi-Fi"] },
                new MeetingRoom { Id = 3, Name = "სასწავლო ცენტრი", Location = "II სართული", Capacity = 40, RequiresApproval = true,
                    Equipment = ["პროექტორი", "მიკროფონი", "დინამიკები", "სასწავლო მაგიდები", "Wi-Fi"] },
                new MeetingRoom { Id = 4, Name = "სტუდია", Location = "I სართული", Capacity = 6, RequiresApproval = true,
                    Equipment = ["კამერა", "განათება", "მიკროფონი", "ხმის ჩამწერი"] },
                new MeetingRoom { Id = 5, Name = "კლუბი", Location = "ცოკოლი", Capacity = 60, RequiresApproval = true,
                    Equipment = ["სცენა", "ხმის სისტემა", "პროექტორი", "სავარძლები"] }
            );
        });

        //Booking 
        modelBuilder.Entity<Booking>(e =>
        {
            e.Property(b => b.Title).HasMaxLength(120).IsRequired();

            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne<MeetingRoom>()
                .WithMany()
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(b => b.Participants)
                .WithOne()
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(b => new { b.RoomId, b.Date });
        });

        //BookingParticipant
        modelBuilder.Entity<BookingParticipant>(e =>
            e.HasKey(p => new { p.BookingId, p.EmployeeId }));
    }
}