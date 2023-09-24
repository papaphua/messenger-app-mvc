﻿namespace MessengerApp.Domain.Enumerations;

public enum Results
{
    InvalidLink,
    UserNotFound,
    ProfileUpdated,
    ProfilePictureUpdate,
    PasswordChanged,
    EmailAlreadyConfirmed,
    EmailConfirmed,
    EmailChanged,
    EmailSameAsCurrent,
    EmailChangeError,
    ExternalUserPasswordError,
    ExternalUserEmailError,
    MessageSendError,
    ChatNotFound,
    ChatsEmpty,
    ChatCreateError,
    ChatRemoveError,
    ChatLeaveError,
    ChatJoinError,
    ChatAlreadyMember,
    AlreadyReacted,
    ReactionsNotAllowed,
    CommentsNotAllowed,
    EmailAlreadyTaken,
    EmailChangeRequestSentTo,
    EmailConfirmationRequestSentTo,
    NoSearchResultsFor
}