using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Postgrest.Tests.Models;

[Table("movie")]
public class Movie : BaseModel
{
    [PrimaryKey("id")] public string Id { get; set; } = null!;

    [Column("name")] public string? Name { get; set; }

    [Column("status")] public MovieStatus? Status { get; set; }

    [Reference(typeof(Person), ReferenceAttribute.JoinType.Left)]
    public List<Person> People { get; set; } = new();

    [Column("created_at")] public DateTime CreatedAt { get; set; }
}

public enum MovieStatus
{
    OnDisplay,
    OffDisplay
}

[Table("person")]
public class Person : BaseModel
{
    [PrimaryKey("id")] public string Id { get; set; } = null!;

    [Reference(typeof(Movie))] public List<Movie> Movies { get; set; } = new();

    [Reference(typeof(Profile))]
    public Profile? Profile { get; set; }

    [Column("first_name")] public string? FirstName { get; set; }

    [Column("last_name")] public string? LastName { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }
}

[Table("profile")]
public class Profile : BaseModel
{
    [PrimaryKey("person_id", true)] public string PersonId { get; set; } = null!;

    [Reference(typeof(Person))] public Person? Person { get; set; }
    [Column("email")] public string? Email { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }
}

[Table("movie_person")]
public class MoviePerson : BaseModel
{
    [Column("movie_id")] public string? MovieId { get; set; }
    [Column("person_id")] public string? PersonId { get; set; }
}
