import 'dart:async';

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/core/database/api/graph_ql_config.dart';
import 'package:page_ui/core/helpers/app_logger.dart';
import 'package:page_ui/features/chat/domain/constants/message_types.dart';
import 'package:page_ui/features/chat/domain/entities/message_entity.dart';
import 'package:page_ui/features/chat/domain/repos/chat_repo.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/_chat_session.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_state.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_typewriter_registry.dart';

class ChatMessagesCubit extends Cubit<ChatMessagesState> {
  ChatMessagesCubit({required ChatRepo chatRepo})
    : _chatRepo = chatRepo,
      super(const ChatMessagesInitial());

  static const int _pageSize = 16;

  final ChatRepo _chatRepo;
  final Map<String, ChatSession> _sessions = {};
  String? _activeChatId;

  ChatSession _session(String chatId) =>
      _sessions.putIfAbsent(chatId, ChatSession.new);

  

  Future<void> openChat({required String chatId}) async {
    _activeChatId = chatId;
    final session = _session(chatId);

    if (session.isHydrated) {
      _emitLoaded(chatId);
      return;
    }

    if (session.isLoading) {
      emit(ChatMessagesLoading(chatId: chatId));
      return;
    }

    session.isLoading = true;
    emit(ChatMessagesLoading(chatId: chatId));

    final result = await _chatRepo.getMessages(
      chatId: chatId,
      first: _pageSize,
    );
    if (isClosed) return;
    session.isLoading = false;

    result.fold(
      (failure) {
        if (_activeChatId == chatId) {
          emit(ChatMessagesError(chatId: chatId, message: failure.message));
        }
      },
      (response) {
        session.isHydrated = true;
        session.messages = _mergeMessages(session.messages, response.messages);
        session.hasNextPage = response.hasNextPage;
        session.endCursor = response.endCursor;

        if (_activeChatId == chatId) _emitLoaded(chatId);
      },
    );
  }

  Future<void> refreshMessages({required String chatId}) async {
    final session = _session(chatId);
    session.isHydrated = false;
    session.messages = [];
    session.endCursor = null;
    session.hasNextPage = false;
    await openChat(chatId: chatId);
  }

  Future<void> closeChat({required String chatId}) async {
    await _cancelSubscription(chatId);
    if (_activeChatId == chatId) {
      _activeChatId = null;
    }
    final anySubLeft = _sessions.values.any((s) => s.subscription != null);
    if (!anySubLeft) {
      await GraphQLConfig.disconnectWebSocket();
    }
  }

  

  
  
  
  
  
  void startMessageSubscription(String chatId) {
    
    
    
    
    _activeChatId = chatId;
    final session = _session(chatId);

    
    _cancelSubscription(chatId);

    session.isSubscriptionActive = true;
    session.isAwaitingAiResponse = true;
    session.activeThinkingMessage = null;
    if (_activeChatId == chatId) _emitLoaded(chatId);

    
    session.subscription = _chatRepo
        .subscribeToMessages(chatId: chatId)
        .listen(
          (message) {
            if (isClosed) return;
            _handleIncomingMessage(chatId, session, message);
          },
          onError: (Object error, StackTrace stackTrace) {
            appLogger.e(
              'Message subscription error for $chatId',
              error: error,
              stackTrace: stackTrace,
            );
            if (isClosed) return;
            _closeSubscriptionInternal(chatId, session);
            if (_activeChatId != chatId) return;
            if (session.messages.isEmpty) {
              emit(
                ChatMessagesError(
                  chatId: chatId,
                  message: 'Live message stream interrupted: $error',
                ),
              );
            } else {
              _emitLoaded(chatId);
            }
          },
          onDone: () {
            if (isClosed) return;
            _closeSubscriptionInternal(chatId, session);
            if (_activeChatId == chatId) _emitLoaded(chatId);
          },
        );
  }

  void _handleIncomingMessage(
    String chatId,
    ChatSession session,
    MessageEntity message,
  ) {
    final type = message.type.trim().toUpperCase();

    if (type == MessageType.thinking) {
      
      session.activeThinkingMessage = message;
      session.isAwaitingAiResponse = true;
    } else if (type == MessageType.aiMessage) {
      
      session.activeThinkingMessage = null;
      session.isAwaitingAiResponse = false;
      session.messages = _mergeMessages(session.messages, [message]);
      ChatTypewriterRegistry.markArrived(message.id);
    } else if (type == MessageType.aiRun) {
      
      session.activeThinkingMessage = null;
      session.isAwaitingAiResponse = false;
      final isNew = !session.messages.any((m) => m.id == message.id);
      session.messages = _mergeMessages(session.messages, [message]);
      if (isNew) ChatTypewriterRegistry.markArrived(message.id);
      _closeSubscriptionInternal(chatId, session);

      
      
      _fetchMessagesSilently(chatId, session);
    } else if (type == MessageType.userMessage) {
      
      
      final optimisticId = session.optimisticMessageId;
      if (optimisticId != null) {
        session.messages =
            session.messages.where((m) => m.id != optimisticId).toList();
        session.optimisticMessageId = null;
      }
      session.messages = _mergeMessages(session.messages, [message]);
    } else {
      
      session.messages = _mergeMessages(session.messages, [message]);
    }

    if (_activeChatId == chatId) _emitLoaded(chatId);
  }

  
  void _closeSubscriptionInternal(String chatId, ChatSession session) {
    final sub = session.subscription;
    session.subscription = null;
    session.isSubscriptionActive = false;
    sub?.cancel();

    
    final anySubLeft = _sessions.values.any((s) => s.subscription != null);
    if (!anySubLeft) {
      GraphQLConfig.disconnectWebSocket();
    }
  }

  Future<void> _cancelSubscription(String chatId) async {
    final session = _sessions[chatId];
    final sub = session?.subscription;
    session?.subscription = null;
    session?.isSubscriptionActive = false;
    if (sub != null) await sub.cancel();
  }

  

  Future<void> loadMoreMessages({required String chatId}) async {
    final session = _sessions[chatId];
    if (session == null) return;
    if (session.isLoadingMore || !session.hasNextPage) return;
    final endCursor = session.endCursor;
    if (endCursor == null) return;

    session.isLoadingMore = true;
    if (_activeChatId == chatId) _emitLoaded(chatId);

    final result = await _chatRepo.getMessages(
      chatId: chatId,
      first: _pageSize,
      after: endCursor,
    );
    if (isClosed) return;
    session.isLoadingMore = false;

    result.fold(
      (_) {
        if (_activeChatId == chatId) _emitLoaded(chatId);
      },
      (response) {
        session.messages = _mergeMessages(session.messages, response.messages);
        session.hasNextPage = response.hasNextPage;
        session.endCursor = response.endCursor;
        if (_activeChatId == chatId) _emitLoaded(chatId);
      },
    );
  }

  

  Future<void> _fetchMessagesSilently(String chatId, ChatSession session) async {
    try {
      final result = await _chatRepo.getMessages(chatId: chatId, first: 20);
      if (isClosed) return;
      result.fold(
        (failure) {
          appLogger.e('Silent fetch failed: ${failure.message}');
        },
        (response) {
          session.isHydrated = true;

          
          final hasRealUserMessage =
              response.messages.any((m) => m.type == MessageType.userMessage);
          final optId = session.optimisticMessageId;
          if (hasRealUserMessage && optId != null) {
            session.messages =
                session.messages.where((m) => m.id != optId).toList();
            session.optimisticMessageId = null;
          }

          session.messages = _mergeMessages(session.messages, response.messages);
          if (_activeChatId == chatId) _emitLoaded(chatId);
        },
      );
    } catch (e) {
      appLogger.e('Silent fetch error', error: e);
    }
  }

  
  
  
  
  void addOutgoingMessage({
    required String chatId,
    required String content,
    String? attachmentUrl,
  }) {
    final tempId = 'opt_${DateTime.now().millisecondsSinceEpoch}';
    final session = _session(chatId);
    session.optimisticMessageId = tempId;
    final optimistic = MessageEntity(
      id: tempId,
      chatId: chatId,
      content: content,
      type: MessageType.userMessage,
      status: 'sent',
      createdAt: DateTime.now(),
      attachmentUrl: attachmentUrl,
      isQuestion: true,
    );
    session.messages = _mergeMessages(session.messages, [optimistic]);
    if (_activeChatId == chatId) _emitLoaded(chatId);
  }

  void selectAiRunMessage({required String chatId, required String messageId}) {
    final session = _sessions[chatId];
    if (session == null) return;
    session.selectedAiRunId = messageId;
    if (_activeChatId == chatId && session.isHydrated) _emitLoaded(chatId);
  }

  void markAwaitingAiResponse({required String chatId}) {
    final session = _session(chatId);
    session.isAwaitingAiResponse = true;
    if (_activeChatId == chatId && session.isHydrated) _emitLoaded(chatId);
  }

  void _emitLoaded(String chatId) {
    final session = _session(chatId);
    final sorted = [...session.messages]
      ..sort((a, b) => a.createdAt.compareTo(b.createdAt));

    emit(
      ChatMessagesLoaded(
        chatId: chatId,
        messages: List.unmodifiable(sorted),
        hasNextPage: session.hasNextPage,
        endCursor: session.endCursor,
        isLoadingMore: session.isLoadingMore,
        selectedAiRunMessageId: session.selectedAiRunId,
        isAwaitingAiResponse: session.isAwaitingAiResponse,
        activeThinkingMessage: session.activeThinkingMessage,
        isSubscriptionActive: session.isSubscriptionActive,
      ),
    );
  }

  List<MessageEntity> _mergeMessages(
    List<MessageEntity> existing,
    Iterable<MessageEntity> incoming,
  ) {
    final byId = <String, MessageEntity>{for (final m in existing) m.id: m};
    for (final m in incoming) {
      byId[m.id] = m;
    }
    return byId.values.toList(growable: false);
  }

  @override
  Future<void> close() async {
    for (final session in _sessions.values) {
      final sub = session.subscription;
      session.subscription = null;
      if (sub != null) await sub.cancel();
    }
    _sessions.clear();
    return super.close();
  }
}
