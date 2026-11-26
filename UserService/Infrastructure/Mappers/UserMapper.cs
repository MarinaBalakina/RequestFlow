using Riok.Mapperly.Abstractions;
using UserService.Domain.Entity;
using UserService.Infrastructure.Models;

namespace UserService.Infrastructure.Mappers;

[Mapper]
public sealed partial class UserMapper
{
    public partial UserDbModel UserToUserDbModel(User user);
    public partial User UserDbModelToUser(UserDbModel model);
}
