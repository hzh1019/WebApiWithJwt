namespace WebApi02
{
    public interface IUserService
    {
        bool IsValid(LoginRequestDTO req);
    }
}