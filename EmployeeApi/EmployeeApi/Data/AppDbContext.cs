using EmployeeApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EmployeeApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<MeetingRoom> MeetingRooms => Set<MeetingRoom>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingParticipant> BookingParticipants => Set<BookingParticipant>();
    public DbSet<Pass> Passes => Set<Pass>();
    public DbSet<PassVisitor> PassVisitors => Set<PassVisitor>();

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
                new MeetingRoom { Id = 1, Name = "შეხვედრის ოთახი ცენტრალური", Location = "IV სართული", Capacity = 20, RequiresApproval = true,
                    Equipment = ["პროექტორი", "ეკრანი", "კონფერენც ტელეფონი", "თეთრი დაფა", "Wi-Fi"] },
                new MeetingRoom { Id = 2, Name = "შეხვედრის ოთახი სატვირთო", Location = "III სართული", Capacity = 12, RequiresApproval = true,
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

        modelBuilder.Entity<Employee>(e =>
        {

            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            
            e.Property(x => x.Position).HasMaxLength(100).IsRequired();

            e.Property(x => x.Email).HasMaxLength(120).IsRequired();

            e.Property(x => x.PhotoFileName).HasMaxLength(120);


            e.HasData(
                new Employee { Id = 1, Name = "Davit Khvedelidze", Position = "Manager",   Email = "davit.khvedelidze@example.com" },
                new Employee { Id = 2, Name = "Giorgi Beridze",    Position = "Developer", Email = "giorgi.beridze@example.com" },
                new Employee { Id = 3, Name = "Nino Kapanadze",    Position = "Designer",  Email = "nino.kapanadze@example.com" }
            );

        });

        //BookingParticipant
        modelBuilder.Entity<BookingParticipant>(e =>
            e.HasKey(p => new { p.BookingId, p.EmployeeId }));

        //Pass
        modelBuilder.Entity<Pass>(e =>
        {
            e.Property(p => p.Purpose).HasMaxLength(200).IsRequired();
            e.Property(p => p.Room).HasMaxLength(20).IsRequired();
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(p => p.InitiatorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(p => p.Visitors)
                .WithOne()
                .HasForeignKey(v => v.PassId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => p.VisitDate);

            e.HasData(
                new Pass { Id = 1, InitiatorId = 2, VisitDate = new DateOnly(2026, 10, 9), Purpose = "გაცნობითი შეხვედრა", Room = "101",
                    Status = PassStatus.Pending, CreatedAt = new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc) },
                new Pass { Id = 2, InitiatorId = 1, VisitDate = new DateOnly(2026, 10, 7), Purpose = "ტექნიკური კონსულტაცია", Room = "203",
                    Status = PassStatus.Pending, CreatedAt = new DateTime(2026, 10, 1, 11, 30, 0, DateTimeKind.Utc) },
                new Pass { Id = 3, InitiatorId = 3, VisitDate = new DateOnly(2026, 10, 6), Purpose = "გასაუბრება", Room = "305",
                    Status = PassStatus.Issued, IssuedDate = new DateOnly(2026, 10, 5), CreatedAt = new DateTime(2026, 9, 30, 8, 15, 0, DateTimeKind.Utc) },
                new Pass { Id = 4, InitiatorId = 2, VisitDate = new DateOnly(2026, 10, 3), Purpose = "გაცნობითი შეხვედრა", Room = "101",
                    Status = PassStatus.Issued, IssuedDate = new DateOnly(2026, 10, 3), CreatedAt = new DateTime(2026, 9, 29, 14, 0, 0, DateTimeKind.Utc) },
                new Pass { Id = 5, InitiatorId = 1, VisitDate = new DateOnly(2026, 9, 29), Purpose = "პარტნიორთან შეხვედრა", Room = "402",
                    Status = PassStatus.Completed, IssuedDate = new DateOnly(2026, 9, 29), CreatedAt = new DateTime(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc) },
                new Pass { Id = 6, InitiatorId = 3, VisitDate = new DateOnly(2026, 9, 22), Purpose = "აუდიტი", Room = "203",
                    Status = PassStatus.Completed, IssuedDate = new DateOnly(2026, 9, 22), CreatedAt = new DateTime(2026, 9, 17, 12, 45, 0, DateTimeKind.Utc) },
                new Pass { Id = 7, InitiatorId = 2, VisitDate = new DateOnly(2026, 9, 18), Purpose = "მომწოდებელთან შეხვედრა", Room = "101",
                    Status = PassStatus.Cancelled, CreatedAt = new DateTime(2026, 9, 15, 9, 20, 0, DateTimeKind.Utc) }
            );
        });

        //PassVisitor
        modelBuilder.Entity<PassVisitor>(e =>
        {
            e.Property(v => v.FullName).HasMaxLength(100).IsRequired();
            e.Property(v => v.PersonalNumber).HasMaxLength(30).IsRequired();
            e.Property(v => v.Residency).HasConversion<string>().HasMaxLength(20);
            e.Property(v => v.DocumentType).HasConversion<string>().HasMaxLength(30);
            e.Property(v => v.CardNumber).HasMaxLength(30);
            e.Property(v => v.Note).HasMaxLength(300);

            e.HasData(
                new PassVisitor { Id = 1, PassId = 1, FullName = "ანა მელაძე", PersonalNumber = "01024056789",
                    Residency = Residency.Resident, DocumentType = DocumentType.IdCard },
                new PassVisitor { Id = 2, PassId = 1, FullName = "ლევან ჯაფარიძე", PersonalNumber = "01001023456",
                    Residency = Residency.Resident, DocumentType = DocumentType.IdCard, IsCompanion = true, Note = "თანმხლები პირი" },
                new PassVisitor { Id = 3, PassId = 1, FullName = "John Smith", PersonalNumber = "GB5521873",
                    Residency = Residency.NonResident, DocumentType = DocumentType.InternationalPassport },
                new PassVisitor { Id = 4, PassId = 2, FullName = "თამარ ნოზაძე", PersonalNumber = "61004011223",
                    Residency = Residency.Resident, DocumentType = DocumentType.IdCard },
                new PassVisitor { Id = 5, PassId = 3, FullName = "საბა კიკნაძე", PersonalNumber = "35001098765",
                    Residency = Residency.Resident, DocumentType = DocumentType.IdCard, CardNumber = "V-0142", PassIssued = true },
                new PassVisitor { Id = 6, PassId = 3, FullName = "მარიამ ხუციშვილი", PersonalNumber = "01017034512",
                    Residency = Residency.Resident, DocumentType = DocumentType.IdCard, CardNumber = "V-0143", PassIssued = true,
                    IsCompanion = true, Note = "თანმხლები პირი" },
                new PassVisitor { Id = 7, PassId = 4, FullName = "Elena Rossi", PersonalNumber = "YA8830214",
                    Residency = Residency.NonResident, DocumentType = DocumentType.InternationalPassport, CardNumber = "V-0139", PassIssued = true },
                new PassVisitor { Id = 8, PassId = 5, FullName = "გიორგი ლომიძე", PersonalNumber = "01008045671",
                    Residency = Residency.Resident, DocumentType = DocumentType.IdCard, CardNumber = "V-0127", PassIssued = true },
                new PassVisitor { Id = 9, PassId = 6, FullName = "ნათია ბერიძე", PersonalNumber = "01019062234",
                    Residency = Residency.Resident, DocumentType = DocumentType.IdCard, CardNumber = "V-0118", PassIssued = true },
                new PassVisitor { Id = 10, PassId = 6, FullName = "დავით ცერცვაძე", PersonalNumber = "01027011890",
                    Residency = Residency.Resident, DocumentType = DocumentType.IdCard, CardNumber = "V-0119", PassIssued = true },
                new PassVisitor { Id = 11, PassId = 7, FullName = "ირაკლი გოგოლაძე", PersonalNumber = "01031077432",
                    Residency = Residency.Resident, DocumentType = DocumentType.IdCard, Note = "ვიზიტი გადაიდო" }
            );
        });
    }
}