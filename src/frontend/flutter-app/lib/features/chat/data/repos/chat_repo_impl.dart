import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/exceptions.dart';
import 'package:page_ui/core/errors/failure.dart';
import 'package:page_ui/core/helpers/app_logger.dart';
import 'package:page_ui/core/network/network_info.dart';
import 'package:page_ui/features/chat/data/data_source/abstract_chat_data_source.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/domain/entities/message_entity.dart';
import 'package:page_ui/features/chat/domain/params/create_chat_params.dart';
import 'package:page_ui/features/chat/domain/params/send_message_params.dart';
import 'package:page_ui/features/chat/domain/repos/chat_repo.dart';
import 'package:dartz/dartz.dart';

class ChatRepoImpl extends ChatRepo {
  final ChatDataSource dataSource;
  final NetworkInfo networkInfo;

  ChatRepoImpl({required this.dataSource, required this.networkInfo});

  Future<Either<Failure, T>> _guardNetworkConnection<T>(
    AppOperation operation,
    Future<T> Function() action,
  ) async {
    try {
      if (!await networkInfo.isConnected) {
        return Left(NetworkFailure.error());
      }
      return Right(await action());
    } on ServerException catch (e, stackTrace) {
      appLogger.e(
        'ChatRepo.${operation.name} failed',
        error: e,
        stackTrace: stackTrace,
      );
      return Left(ServerFailure.fromException(e));
    } on CacheExeption catch (e, stackTrace) {
      appLogger.e(
        'ChatRepo.${operation.name} cache failed',
        error: e,
        stackTrace: stackTrace,
      );
      return Left(CacheFailure.fromException(e));
    } catch (e, stackTrace) {
      appLogger.e(
        'ChatRepo.${operation.name} unexpected',
        error: e,
        stackTrace: stackTrace,
      );
      return Left(ServerFailure.forOperation(operation));
    }
  }

  @override
  Future<Either<Failure, ChatEntity>> createChat({
    required CreateChatParams params,
  }) {
    return _guardNetworkConnection(
      AppOperation.createChat,
      () => dataSource.createChat(params: params),
    );
  }

  @override
  Future<
    Either<
      Failure,
      ({List<ChatEntity> chats, bool hasNextPage, String? endCursor})
    >
  >
  getChats({required int first, String? after}) {
    return _guardNetworkConnection(
      AppOperation.loadChats,
      () => dataSource.getChats(first: first, after: after),
    );
  }

  @override
  Future<Either<Failure, List<ChatEntity>>> searchChats({
    required String name,
    required int first,
  }) {
    return _guardNetworkConnection(
      AppOperation.searchChats,
      () => dataSource.searchChats(name: name, first: first),
    );
  }

  @override
  Future<
    Either<
      Failure,
      ({List<MessageEntity> messages, bool hasNextPage, String? endCursor})
    >
  >
  getMessages({required String chatId, required int first, String? after}) {
    return _guardNetworkConnection(
      AppOperation.loadMessages,
      () => dataSource.getMessages(chatId: chatId, first: first, after: after),
    );
  }

  @override
  Stream<MessageEntity> subscribeToMessages({required String chatId}) {
    return dataSource.subscribeToMessages(chatId: chatId);
  }

  @override
  Future<Either<Failure, void>> sendMessage({
    required SendMessageParams params,
  }) {
    return _guardNetworkConnection(
      AppOperation.sendMessage,
      () => dataSource.sendMessage(params: params),
    );
  }

  @override
  Future<Either<Failure, void>> deleteChat({required String chatId}) {
    return _guardNetworkConnection(
      AppOperation.deleteChat,
      () => dataSource.deleteChat(chatId: chatId),
    );
  }

  @override
  Future<Either<Failure, ChatEntity>> renameChat({
    required String chatId,
    required String name,
  }) {
    return _guardNetworkConnection(
      AppOperation.renameChat,
      () => dataSource.renameChat(chatId: chatId, name: name),
    );
  }
}
