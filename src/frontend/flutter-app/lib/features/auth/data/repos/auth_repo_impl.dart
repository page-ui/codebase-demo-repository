import 'package:page_ui/core/database/api/graph_ql_config.dart';
import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/exceptions.dart';
import 'package:page_ui/core/errors/failure.dart';
import 'package:page_ui/core/helpers/app_logger.dart';
import 'package:page_ui/core/network/network_info.dart';
import 'package:page_ui/features/auth/data/data_source/auth_data_source.dart';
import 'package:page_ui/features/auth/data/model/user_tokens_model.dart';
import 'package:page_ui/features/auth/domain/params/login_params.dart';
import 'package:page_ui/features/auth/domain/params/register_params.dart';
import 'package:page_ui/features/auth/domain/params/reset_password.dart';
import 'package:page_ui/features/auth/domain/params/verify_reset_code_params.dart';
import 'package:page_ui/features/auth/domain/repos/auth_repo.dart';
import 'package:dartz/dartz.dart';

class AuthRepoImpl extends AuthRepo {
  final AuthDataSource dataSource;
  final NetworkInfo networkInfo;
  AuthRepoImpl({required this.dataSource, required this.networkInfo});

  Future<Either<Failure, T>> _guard<T>(
    AppOperation operation,
    Future<T> Function() action, {
    bool checkNetwork = true,
  }) async {
    try {
      if (checkNetwork && !await networkInfo.isConnected) {
        return Left(NetworkFailure.error());
      }
      return Right(await action());
    } on ServerException catch (e, stackTrace) {
      appLogger.e(
        'AuthRepo.${operation.name} failed',
        error: e,
        stackTrace: stackTrace,
      );
      return Left(ServerFailure.fromException(e));
    } on CacheExeption catch (e, stackTrace) {
      appLogger.e(
        'AuthRepo.${operation.name} cache failed',
        error: e,
        stackTrace: stackTrace,
      );
      return Left(CacheFailure.fromException(e));
    } catch (e, stackTrace) {
      appLogger.e(
        'AuthRepo.${operation.name} unexpected',
        error: e,
        stackTrace: stackTrace,
      );
      return Left(ServerFailure.forOperation(operation));
    }
  }

  @override
  Future<Either<Failure, UserTokensModel>> login({required LoginParams param}) {
    return _guard(AppOperation.login, () async {
      final tokens = await dataSource.login(params: param);
      GraphQLConfig.accessToken = tokens.accessToken;
      GraphQLConfig.refreshToken = tokens.refreshToken;
      return tokens;
    });
  }

  @override
  Future<Either<Failure, bool>> register({required RegisterParams param}) {
    return _guard(
      AppOperation.register,
      () => dataSource.register(params: param),
    );
  }

  @override
  Future<Either<Failure, bool>> forgotPasswordRequest({required String email}) {
    return _guard(
      AppOperation.forgotPassword,
      () => dataSource.forgotPasswordRequest(email: email),
      checkNetwork: false,
    );
  }

  @override
  Future<Either<Failure, String>> verifyResetCode({
    required VerifyResetCodeParams params,
  }) {
    return _guard(
      AppOperation.verifyCode,
      () => dataSource.verifyResetCode(params: params),
      checkNetwork: false,
    );
  }

  @override
  Future<Either<Failure, bool>> emailVerfication({
    required VerifyResetCodeParams params,
  }) {
    return _guard(
      AppOperation.verifyEmail,
      () => dataSource.emailVerfication(params: params),
      checkNetwork: false,
    );
  }

  @override
  Future<Either<Failure, void>> changePassword({
    required ResetPasswordParams params,
  }) {
    return _guard<void>(
      AppOperation.resetPassword,
      () => dataSource.resetPassword(params: params),
      checkNetwork: false,
    );
  }

  @override
  Future<Either<Failure, void>> resendVerficationCode({required String email}) {
    return _guard<void>(
      AppOperation.resendCode,
      () => dataSource.resendVerficationCode(email: email),
      checkNetwork: false,
    );
  }

  @override
  Future<Either<Failure, void>> signOut() async {
    return _guard<void>(AppOperation.signOut, () async {
      if (GraphQLConfig.refreshToken == null) {
        final tokens = await returnTokensFromSecureDB();
        GraphQLConfig.accessToken = tokens.accessToken;
        GraphQLConfig.refreshToken = tokens.refreshToken;
      }

      final currentRefreshToken = GraphQLConfig.refreshToken;
      if (currentRefreshToken == null) {
        throw CacheExeption(
          errorMessage: 'There is an error , please try again.',
        );
      }
      await dataSource.signOut(refreshToken: currentRefreshToken);
      await GraphQLConfig.clearTokens();
    });
  }

  @override
  Future<Either<Failure, bool>> requestAccountDeletion() {
    return _guard(
      AppOperation.requestDeleteAccount,
      () => dataSource.requestAccountDeletion(),
    );
  }

  @override
  Future<Either<Failure, bool>> deleteAccount({required String code}) {
    return _guard(
      AppOperation.deleteAccount,
      () async {
        final result = await dataSource.deleteAccount(code: code);
        if (result) {
          await GraphQLConfig.clearTokens();
        }
        return result;
      },
    );
  }
}
