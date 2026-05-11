import 'package:graphql_flutter/graphql_flutter.dart' hide ServerException;
import 'package:page_ui/core/database/api/graph_ql_config.dart';
import 'package:page_ui/core/database/api/queries.dart';
import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/exceptions.dart';
import 'package:page_ui/features/chat/data/data_source/abstract_chat_data_source.dart';
import 'package:page_ui/features/chat/data/models/chat_model.dart';
import 'package:page_ui/features/chat/data/models/message_model.dart';
import 'package:page_ui/features/chat/domain/params/create_chat_params.dart';
import 'package:page_ui/features/chat/domain/params/send_message_params.dart';

class ChatDataSourceImpl extends ChatDataSource {
  GraphQLClient get _client => GraphQLConfig.client.value;

  @override
  Future<ChatModel> createChat({required CreateChatParams params}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.createChatMutation),
        variables: {'input': params.toInputJson()},
      ),
    );

    if (result.hasException || result.data?['createChat'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.createChat,
      );
    }

    return ChatModel.fromJson(
      result.data!['createChat']["chat"] as Map<String, dynamic>,
    );
  }

  @override
  Future<({List<ChatModel> chats, bool hasNextPage, String? endCursor})>
  getChats({required int first, String? after}) async {
    final result = await _client.query(
      QueryOptions(
        document: gql(Queries.chatRoomsQuery),
        variables: {'first': first, if (after != null) 'after': after},
        fetchPolicy: FetchPolicy.networkOnly,
      ),
    );

    if (result.hasException || result.data?['chats'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.loadChats,
      );
    }

    final data = result.data!['chats'] as Map<String, dynamic>;
    final pageInfo = data['pageInfo'] as Map<String, dynamic>;
    final nodes = data['edges'] as List;

    final chats = nodes
        .map((node) => ChatModel.fromJson(node["node"] as Map<String, dynamic>))
        .toList();

    return (
      chats: chats,
      hasNextPage: pageInfo['hasNextPage'] as bool,
      endCursor: pageInfo['endCursor'] as String?,
    );
  }

  @override
  Future<List<ChatModel>> searchChats({
    required String name,
    required int first,
  }) async {
    final result = await _client.query(
      QueryOptions(
        document: gql(Queries.searchChatQuery),
        variables: {'name': name, 'first': first},
        fetchPolicy: FetchPolicy.networkOnly,
      ),
    );

    if (result.hasException || result.data?['searchChats'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.searchChats,
      );
    }

    final data = result.data!['searchChats'] as Map<String, dynamic>;
    final nodes = data['edges'] as List;

    final chats = nodes
        .map((node) => ChatModel.fromJson(node["node"] as Map<String, dynamic>))
        .toList();

    return chats;
  }

  @override
  Future<({List<MessageModel> messages, bool hasNextPage, String? endCursor})>
  getMessages({
    required String chatId,
    required int first,
    String? after,
  }) async {
    final result = await _client.query(
      QueryOptions(
        document: gql(Queries.getMessagesQuery),
        variables: {
          'chatKey': chatId,
          'first': first,
          if (after != null) 'after': after,
        },
        fetchPolicy: FetchPolicy.networkOnly,
      ),
    );

    if (result.hasException || result.data?['messages'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.loadMessages,
      );
    }

    final data = result.data!['messages'] as Map<String, dynamic>;
    final pageInfo = data['pageInfo'] as Map<String, dynamic>;
    final edges = (data['edges'] as List?) ?? const [];

    final messages = edges
        .map(
          (edge) => MessageModel.fromJson(
            (edge as Map<String, dynamic>)['node'] as Map<String, dynamic>,
            fallbackChatId: chatId,
          ),
        )
        .toList();

    return (
      messages: messages,
      hasNextPage: pageInfo['hasNextPage'] as bool,
      endCursor: pageInfo['endCursor'] as String?,
    );
  }

  @override
  Stream<MessageModel> subscribeToMessages({required String chatId}) async* {
    final stream = _client.subscribe(
      SubscriptionOptions(
        document: gql(Queries.onMessageCreatedSubscription),
        variables: {'chatKey': chatId},
      ),
    );

    await for (final result in stream) {
      if (result.hasException) {
        throw ServerException.fromGraphQL(
          result.exception,
          operation: AppOperation.subscribeMessages,
        );
      }

      final payload = result.data?['onMessageCreated'];
      if (payload == null) {
        continue;
      }

      yield MessageModel.fromJson(
        payload as Map<String, dynamic>,
        fallbackChatId: chatId,
      );
    }
  }

  @override
  Future<void> sendMessage({required SendMessageParams params}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.sendMessageMutation),
        variables: {'input': params.toInputJson()},
      ),
    );

    if (result.hasException || result.data?['createMessage'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.sendMessage,
      );
    }
  }

  @override
  Future<void> deleteChat({required String chatId}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.deleteChatMutation),
        variables: {'chatKey': chatId},
      ),
    );

    if (result.hasException) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.deleteChat,
      );
    }
  }

  @override
  Future<ChatModel> renameChat({
    required String chatId,
    required String name,
  }) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.renameChatMutation),
        variables: {
          'input': {'chatKey': chatId, 'name': name},
        },
      ),
    );

    if (result.hasException || result.data?['renameChat'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.renameChat,
      );
    }

    return ChatModel.fromJson(
      result.data!['renameChat'] as Map<String, dynamic>,
    );
  }
}
