using System.Runtime.Serialization;

namespace Sample.Shared.Exceptions;

[Serializable]
public class AuthorCannotJoinListException : Exception
{
    public AuthorCannotJoinListException() : base("Author cannot join their own list") { }
    public AuthorCannotJoinListException(string message) : base(message) { }
    public AuthorCannotJoinListException(string message, Exception innerException) : base(message, innerException) { }
    protected AuthorCannotJoinListException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
public class CollaboratorCannotRemoveItemFromListException : Exception
{
    public CollaboratorCannotRemoveItemFromListException() : base("Collaborator cannot remove item") { }
    public CollaboratorCannotRemoveItemFromListException(string message) : base(message) { }
    public CollaboratorCannotRemoveItemFromListException(string message, Exception innerException) : base(message, innerException) { }
    protected CollaboratorCannotRemoveItemFromListException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
public class InvalidJoinCodeException : Exception
{
    public InvalidJoinCodeException() : base("Invalid join code") { }
    public InvalidJoinCodeException(string message) : base(message) { }
    public InvalidJoinCodeException(string message, Exception innerException) : base(message, innerException) { }
    protected InvalidJoinCodeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
public class ItemAlreadyExistsException : Exception
{
    public ItemAlreadyExistsException() : base("Item already added") { }
    public ItemAlreadyExistsException(string message) : base(message) { }
    public ItemAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }
    protected ItemAlreadyExistsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
public class ItemDoesNotExistException : Exception
{
    public ItemDoesNotExistException() : base("Item does not exist") { }
    public ItemDoesNotExistException(string message) : base(message) { }
    public ItemDoesNotExistException(string message, Exception innerException) : base(message, innerException) { }
    protected ItemDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
public class ShoppingListDoesNotExist : Exception
{
    public ShoppingListDoesNotExist() : base("Shopping list does not exist") { }
    public ShoppingListDoesNotExist(string message) : base(message) { }
    public ShoppingListDoesNotExist(string message, Exception innerException) : base(message, innerException) { }
    protected ShoppingListDoesNotExist(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
public class UserHasAlreadyJoinedListException : Exception
{
    public UserHasAlreadyJoinedListException() : base("User is already a collaborator") { }
    public UserHasAlreadyJoinedListException(string message) : base(message) { }
    public UserHasAlreadyJoinedListException(string message, Exception innerException) : base(message, innerException) { }
    protected UserHasAlreadyJoinedListException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}