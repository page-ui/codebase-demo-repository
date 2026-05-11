class ChatTypewriterRegistry {
  ChatTypewriterRegistry._();

  static final Set<String> _pending = <String>{};
  static final Set<String> _completed = <String>{};

  static void markArrived(String messageId) {
    _pending.add(messageId);
  }

  static bool shouldAnimate(String messageId) {
    return _pending.contains(messageId) && !_completed.contains(messageId);
  }

  static void markCompleted(String messageId) {
    _completed.add(messageId);
    _pending.remove(messageId);
  }
}
