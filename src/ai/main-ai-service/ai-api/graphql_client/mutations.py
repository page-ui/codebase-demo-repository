CREATE_MESSAGE_MUTATION = """
mutation CreateMessage($input: CreateMessageInput!) {
  createMessage(input: $input) {
    createdAt
    updatedAt
    messageKey
    chatKey
    title
    content
    isQuestion
    type
    status
    replyToKey
    attachmentUrl
    senderType
  }
}
"""

RENAME_CHAT_MUTATION = """
mutation RenameChat($input: RenameChatInput!) {
  renameChat(input: $input) {
    createdAt
    updatedAt
    chatKey
    name
    modelId
  }
}
"""