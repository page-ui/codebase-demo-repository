import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/failure.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/domain/repos/chat_repo.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_history_cubit/chat_history_state.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class ChatHistoryCubit extends Cubit<ChatHistoryState> {
  static const int _pageSize = 15;

  final ChatRepo _chatRepo;

  ChatHistoryCubit({required ChatRepo chatRepo})
    : _chatRepo = chatRepo,
      super(const ChatHistoryInitial());

  Future<void> loadChats() async {
    if (state is ChatHistoryLoading) return;
    emit(const ChatHistoryLoading());
    final result = await _chatRepo.getChats(first: _pageSize);
    if (isClosed) return;
    result.fold(
      (failure) => emit(ChatHistoryFailure(message: failure.message)),
      (data) => emit(
        ChatHistoryLoaded(
          chats: data.chats,
          hasNextPage: data.hasNextPage,
          endCursor: data.endCursor,
        ),
      ),
    );
  }

  Future<void> loadMore() async {
    final current = state;
    if (current is! ChatHistoryLoaded ||
        !current.hasNextPage ||
        current.isLoadingMore) {
      return;
    }

    emit(current.copyWith(isLoadingMore: true));

    final result = await _chatRepo.getChats(
      first: _pageSize,
      after: current.endCursor,
    );
    if (isClosed) return;

    result.fold(
      (failure) => emit(current.copyWith(isLoadingMore: false)),
      (data) => emit(
        ChatHistoryLoaded(
          chats: [...current.chats, ...data.chats],
          hasNextPage: data.hasNextPage,
          endCursor: data.endCursor,
        ),
      ),
    );
  }

  Future<Either<Failure, void>> deleteChat({required String chatId}) async {
    final current = state;
    final result = await _chatRepo.deleteChat(chatId: chatId);
    if (isClosed) return const Right(null);

    return result.fold((failure) => Left(failure), (_) {
      if (current is ChatHistoryLoaded) {
        emit(
          current.copyWith(
            chats: current.chats.where((c) => c.id != chatId).toList(),
          ),
        );
      }
      return const Right(null);
    });
  }

  Future<Either<Failure, ChatEntity>> renameChat({
    required String chatId,
    required String name,
  }) async {
    final current = state;
    final result = await _chatRepo.renameChat(chatId: chatId, name: name);
    if (isClosed)
      return Left(ServerFailure.forOperation(AppOperation.renameChat));

    return result.fold((failure) => Left(failure), (renamed) {
      if (current is ChatHistoryLoaded) {
        final updated = current.chats.map((c) {
          if (c.id != chatId) return c;
          return ChatEntity(
            id: c.id,
            name: renamed.name,
            createdAt: c.createdAt,
          );
        }).toList();
        emit(current.copyWith(chats: updated));
      }
      return Right(renamed);
    });
  }

  Future<void> searchChats({required String query}) async {
    if (query.isEmpty) return loadChats();

    emit(const ChatHistoryLoading());
    final result = await _chatRepo.searchChats(name: query, first: _pageSize);
    if (isClosed) return;
    result.fold(
      (failure) => emit(ChatHistoryFailure(message: failure.message)),
      (data) => emit(
        ChatHistoryLoaded(chats: data, hasNextPage: false, endCursor: null),
      ),
    );
  }
}
