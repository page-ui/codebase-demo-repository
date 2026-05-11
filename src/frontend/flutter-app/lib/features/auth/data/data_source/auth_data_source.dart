import 'package:page_ui/core/database/api/graph_ql_config.dart';
import 'package:page_ui/core/database/api/queries.dart';
import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/exceptions.dart';
import 'package:page_ui/features/auth/data/model/user_tokens_model.dart';
import 'package:page_ui/features/auth/domain/params/login_params.dart';
import 'package:page_ui/features/auth/domain/params/register_params.dart';
import 'package:page_ui/features/auth/domain/params/reset_password.dart';
import 'package:page_ui/features/auth/domain/params/verify_reset_code_params.dart';
import 'package:graphql_flutter/graphql_flutter.dart' hide ServerException;

abstract class AuthDataSource {
  Future<UserTokensModel> login({required LoginParams params});
  Future<bool> register({required RegisterParams params});
  Future<bool> forgotPasswordRequest({required String email});
  Future<bool> resetPassword({required ResetPasswordParams params});
  Future<String> verifyResetCode({required VerifyResetCodeParams params});
  Future<bool> emailVerfication({required VerifyResetCodeParams params});
  Future<void> resendVerficationCode({required String email});
  Future<void> signOut({required String refreshToken});
  Future<bool> requestAccountDeletion();
  Future<bool> deleteAccount({required String code});
}

class AuthDataSourceImpl extends AuthDataSource {
  final GraphQLClient _client = GraphQLConfig.client.value;
  AuthDataSourceImpl();

  @override
  Future<UserTokensModel> login({required LoginParams params}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.loginMutation),
        variables: {
          'input': {'email': params.email, 'password': params.password},
        },
      ),
    );

    if (result.hasException ||
        result.data == null ||
        !result.data!.containsKey('login') ||
        result.data!['login'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.login,
      );
    }

    final tokens = UserTokensModel.fromJson(result.data!['login']);
    await saveTokens(tokens);

    GraphQLConfig.accessToken = tokens.accessToken;
    GraphQLConfig.refreshToken = tokens.refreshToken;

    return tokens;
  }

  @override
  Future<bool> register({required RegisterParams params}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.registerMutation),
        variables: {
          'input': {
            'email': params.email,
            'password': params.password,
            'name': params.userName,
          },
        },
      ),
    );
    if (result.hasException ||
        result.data == null ||
        !result.data!.containsKey('register') ||
        result.data!['register'] == null ||
        result.data!['register'] == false) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.register,
      );
    }

    return result.data!['register'];
  }

  @override
  Future<bool> forgotPasswordRequest({required String email}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.forgotPasswordRequestMutation),
        variables: {'email': email},
      ),
    );
    if (result.hasException ||
        result.data == null ||
        !result.data!.containsKey('forgotPasswordRequest') ||
        result.data!['forgotPasswordRequest'] == null ||
        result.data!['forgotPasswordRequest'] == false) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.forgotPassword,
      );
    }

    return result.data!['forgotPasswordRequest'];
  }

  @override
  Future<String> verifyResetCode({
    required VerifyResetCodeParams params,
  }) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.verifyResetCodeMutation),
        variables: {'email': params.email, 'code': params.code},
      ),
    );
    if (result.hasException ||
        result.data == null ||
        !result.data!.containsKey('verifyResetCode') ||
        result.data!['verifyResetCode'] == null ||
        result.data!['verifyResetCode'] == false) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.verifyCode,
      );
    }
    return result.data!['verifyResetCode'];
  }

  @override
  Future<bool> emailVerfication({required VerifyResetCodeParams params}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.emailVerficationMutation),
        variables: {'email': params.email, 'code': params.code},
      ),
    );
    if (result.hasException ||
        result.data == null ||
        !result.data!.containsKey('verifyEmail') ||
        result.data!['verifyEmail'] == null ||
        result.data!['verifyEmail'] == false) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.verifyEmail,
      );
    }
    return result.data!['verifyEmail'];
  }

  @override
  Future<bool> resetPassword({required ResetPasswordParams params}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.resetPasswordMutation),
        variables: params.toJson(),
      ),
    );
    if (result.hasException ||
        result.data == null ||
        result.data!['resetPassword'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.resetPassword,
      );
    }
    return result.data!['resetPassword'];
  }

  @override
  Future<void> resendVerficationCode({required String email}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.resendVerificationMutation),
        variables: {"email": email},
      ),
    );
    if (result.hasException) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.resendCode,
      );
    }
  }

  @override
  Future<void> signOut({required String refreshToken}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.signOutMutation),
        variables: {"refreshToken": refreshToken},
      ),
    );
    if (result.hasException) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.signOut,
      );
    }
  }

  @override
  Future<bool> requestAccountDeletion() async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.requestDeleteMutation),
      ),
    );
    if (result.hasException ||
        result.data == null ||
        !result.data!.containsKey('requestAccountDeletion') ||
        result.data!['requestAccountDeletion'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.requestDeleteAccount,
      );
    }
    return result.data!['requestAccountDeletion'];
  }

  @override
  Future<bool> deleteAccount({required String code}) async {
    final result = await _client.mutate(
      MutationOptions(
        document: gql(Queries.deleteAccountMutation),
        variables: {"code": code},
      ),
    );
    
    if (result.hasException ||
        result.data == null ||
        !result.data!.containsKey('deleteAccount') ||
        result.data!['deleteAccount'] == null) {
      throw ServerException.fromGraphQL(
        result.exception,
        operation: AppOperation.deleteAccount,
      );
    }
    return result.data!['deleteAccount'];
  }
}
