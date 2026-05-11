from enum import StrEnum


class MessageType(StrEnum):
    TEXT = "TEXT"
    IMAGE = "IMAGE"
    FILE = "FILE"
    AI_RUN = "AI_RUN"
    USER_MESSAGE = "USER_MESSAGE"
    AI_MESSAGE = "AI_MESSAGE"
    THINKING = "THINKING"


class MessageStatus(StrEnum):
    SENT = "SENT"
    DELIVERED = "DELIVERED"
    READ = "READ"
    FAILED = "FAILED"


class SortEnumType(StrEnum):
    ASC = "ASC"
    DESC = "DESC"