sealed class SendMessageState {
  const SendMessageState();
}

final class SendMessageInitial extends SendMessageState {
  const SendMessageInitial();
}

final class SendMessageLoading extends SendMessageState {
  const SendMessageLoading();
}

final class SendMessageSuccess extends SendMessageState {
  const SendMessageSuccess();
}

final class SendMessageError extends SendMessageState {
  final String message;

  const SendMessageError({required this.message});
}
