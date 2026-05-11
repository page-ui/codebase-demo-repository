class MessageType {
  MessageType._();

  static const String userMessage = 'USER_MESSAGE';
  static const String thinking = 'THINKING';
  static const String aiMessage = 'AI_MESSAGE';
  static const String aiRun = 'AI_RUN';
}


bool isAiRunType(String value) =>
    value.trim().toUpperCase() == MessageType.aiRun;


bool isAiMessageType(String value) =>
    value.trim().toUpperCase() == MessageType.aiMessage;


bool isThinkingType(String value) =>
    value.trim().toUpperCase() == MessageType.thinking;


bool isAssistantMessage(String value) =>
    isAiRunType(value) || isAiMessageType(value);
