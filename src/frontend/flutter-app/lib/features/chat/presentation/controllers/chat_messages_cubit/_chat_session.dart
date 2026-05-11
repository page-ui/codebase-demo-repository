import 'dart:async';

import 'package:page_ui/features/chat/domain/entities/message_entity.dart';

class ChatSession {
  List<MessageEntity> messages = const [];
  bool hasNextPage = false;
  String? endCursor;
  
  StreamSubscription<MessageEntity>? subscription;
  String? selectedAiRunId;
  
  
  String? optimisticMessageId;
  bool isHydrated = false;
  bool isLoading = false;
  bool isLoadingMore = false;
  bool isAwaitingAiResponse = false;

  
  
  bool isSubscriptionActive = false;

  
  
  MessageEntity? activeThinkingMessage;
}
