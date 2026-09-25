using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EmployeeApi.Data;

public class DateOnlyConverter() : ValueConverter<DateOnly, DateTime>(
    d => d.ToDateTime(TimeOnly.MinValue),
    d => DateOnly.FromDateTime(d));

public class TimeOnlyConverter() : ValueConverter<TimeOnly, TimeSpan>(
    t => t.ToTimeSpan(),
    t => TimeOnly.FromTimeSpan(t));