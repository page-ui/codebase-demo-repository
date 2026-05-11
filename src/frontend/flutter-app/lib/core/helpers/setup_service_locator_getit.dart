import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:get_it/get_it.dart';
import 'package:page_ui/core/database/api/graph_ql_config.dart';
import 'package:page_ui/core/network/network_info.dart';
import 'package:page_ui/features/auth/data/data_source/auth_data_source.dart';
import 'package:page_ui/features/auth/data/repos/auth_repo_impl.dart';
import 'package:page_ui/features/chat/data/data_source/abstract_chat_data_source.dart';
import 'package:page_ui/features/chat/data/data_source/chat_data_source.dart';
import 'package:page_ui/features/chat/data/data_source/upload_service.dart';
import 'package:page_ui/features/chat/data/repos/chat_repo_impl.dart';
import 'package:page_ui/features/chat/domain/repos/chat_repo.dart';
import 'package:page_ui/features/chat/domain/usecases/create_chat_usecase.dart';
import 'package:page_ui/features/chat/domain/usecases/send_message_usecase.dart';
import 'package:page_ui/features/chat/domain/usecases/upload_attachment_usecase.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_history_cubit/chat_history_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/send_message_cubit/send_message_cubit.dart';

final getit = GetIt.instance;

setUpServiceLocator() {
  
  GraphQLConfig.initializeRestClient();

  
  getit.registerLazySingleton<NetworkInfo>(
    () => NetworkInfoImpl(Connectivity()),
  );

  
  getit.registerLazySingleton<AuthDataSourceImpl>(() => AuthDataSourceImpl());
  getit.registerLazySingleton<AuthRepoImpl>(
    () => AuthRepoImpl(
      networkInfo: getit.get<NetworkInfo>(),
      dataSource: getit.get<AuthDataSourceImpl>(),
    ),
  );

  
  getit.registerLazySingleton<ChatDataSource>(() => ChatDataSourceImpl());
  getit.registerLazySingleton<UploadService>(() => UploadService());
  getit.registerLazySingleton<ChatRepo>(
    () => ChatRepoImpl(
      dataSource: getit.get<ChatDataSource>(),
      networkInfo: getit.get<NetworkInfo>(),
    ),
  );

  
  getit.registerLazySingleton<UploadAttachmentUseCase>(
    () => UploadAttachmentUseCase(uploadService: getit.get<UploadService>()),
  );
  getit.registerLazySingleton<CreateChatUseCase>(
    () => CreateChatUseCase(
      chatRepo: getit.get<ChatRepo>(),
      uploadAttachment: getit.get<UploadAttachmentUseCase>(),
    ),
  );
  getit.registerLazySingleton<SendMessageUseCase>(
    () => SendMessageUseCase(
      chatRepo: getit.get<ChatRepo>(),
      uploadAttachment: getit.get<UploadAttachmentUseCase>(),
    ),
  );

  
  getit.registerFactory<ChatHomeCubit>(
    () => ChatHomeCubit(createChat: getit.get<CreateChatUseCase>()),
  );
  getit.registerFactory<ChatHistoryCubit>(
    () => ChatHistoryCubit(chatRepo: getit.get<ChatRepo>()),
  );
  getit.registerFactory<ChatMessagesCubit>(
    () => ChatMessagesCubit(chatRepo: getit.get<ChatRepo>()),
  );
  getit.registerFactory<SendMessageCubit>(
    () => SendMessageCubit(sendMessage: getit.get<SendMessageUseCase>()),
  );
}
