# Auth migration history

The active email-verification flow uses `PendingRegistration`.

Older migrations that reference `EmailVerificationCode` are retained only as migration history so existing databases can replay the original schema evolution. They are not part of the current runtime model.
