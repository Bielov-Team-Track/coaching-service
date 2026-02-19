using Bogus;

namespace Shared.Testing.Builders;

public class UserBuilder
{
    private readonly Faker _faker = new();
    private Guid _id;
    private string _email;
    private string _firstName;
    private string _lastName;
    private string _passwordHash;
    private bool _emailVerified;

    public UserBuilder()
    {
        _id = Guid.NewGuid();
        _email = _faker.Internet.Email();
        _firstName = _faker.Name.FirstName();
        _lastName = _faker.Name.LastName();
        _passwordHash = "hashed_password";
        _emailVerified = true;
    }

    public UserBuilder WithId(Guid id) { _id = id; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
    public UserBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
    public UserBuilder WithPasswordHash(string hash) { _passwordHash = hash; return this; }
    public UserBuilder WithEmailVerified(bool verified) { _emailVerified = verified; return this; }

    public (Guid Id, string Email, string FirstName, string LastName, string PasswordHash, bool EmailVerified) Build()
    {
        return (_id, _email, _firstName, _lastName, _passwordHash, _emailVerified);
    }
}