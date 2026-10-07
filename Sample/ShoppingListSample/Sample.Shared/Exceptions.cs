using Orleans;
namespace Sample.Shared.Exceptions;

[GenerateSerializer]
public sealed class AuthorCannotJoinListException : Exception
{
    public AuthorCannotJoinListException() : base("AuthorCannotJoinListException") { }
    public AuthorCannotJoinListException(string message) : base(message) { }
}

[GenerateSerializer]
public sealed class CollaboratorCannotRemoveItemFromListException : Exception
{
    public CollaboratorCannotRemoveItemFromListException() : base("CollaboratorCannotRemoveItemFromListException") { }
    public CollaboratorCannotRemoveItemFromListException(string message) : base(message) { }
}

[GenerateSerializer]
public sealed class InvalidJoinCodeException : Exception
{
    public InvalidJoinCodeException() : base("InvalidJoinCodeException") { }
    public InvalidJoinCodeException(string message) : base(message) { }
}

[GenerateSerializer]
public sealed class ItemAlreadyExistsException : Exception
{
    public ItemAlreadyExistsException() : base("ItemAlreadyExistsException") { }
    public ItemAlreadyExistsException(string message) : base(message) { }
}

[GenerateSerializer]
public sealed class ItemDoesNotExistException : Exception
{
    public ItemDoesNotExistException() : base("ItemDoesNotExistException") { }
    public ItemDoesNotExistException(string message) : base(message) { }
}

[GenerateSerializer]
public sealed class ShoppingListDoesNotExist : Exception
{
    public ShoppingListDoesNotExist() : base("ShoppingListDoesNotExist") { }
    public ShoppingListDoesNotExist(string message) : base(message) { }
}

[GenerateSerializer]
public sealed class UserHasAlreadyJoinedListException : Exception
{
    public UserHasAlreadyJoinedListException() : base("UserHasAlreadyJoinedListException") { }
    public UserHasAlreadyJoinedListException(string message) : base(message) { }
}
