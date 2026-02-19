using MassTransit;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Messaging.Contracts.Events.Profiles;
using Shared.Models;

namespace Shared.Messaging.Consumers
{
    public class UserProfileUpdatedConsumer : IConsumer<UserProfileUpdatedEvent>
    {
        private readonly IRepository<UserProfile> _repository;

        public UserProfileUpdatedConsumer(IRepository<UserProfile> repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<UserProfileUpdatedEvent> context)
        {
            var userProfile = context.Message.UserProfile;

            var existingProfile = await _repository.GetByIdAsync(userProfile.Id);
            if (existingProfile != null)
            {
                existingProfile.Name = userProfile.Name;
                existingProfile.Surname = userProfile.Surname;
                existingProfile.ImageUrl = userProfile.ImageUrl;
                existingProfile.Email = userProfile.Email;
                existingProfile.DateOfBirth = userProfile.DateOfBirth;
                existingProfile.IsActive = userProfile.IsActive;
                _repository.Update(existingProfile);
            }
            else
            {
                _repository.Add(userProfile);
            }

            await _repository.SaveChangesAsync();
        }
    }
}
