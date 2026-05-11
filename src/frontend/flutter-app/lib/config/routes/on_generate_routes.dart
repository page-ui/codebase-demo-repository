import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';
import 'package:page_ui/core/helpers/auth_state.dart';
import 'package:page_ui/features/auth/domain/params/login_params.dart';
import 'package:page_ui/features/auth/presentation/views/email_verfication_view.dart';
import 'package:page_ui/features/auth/presentation/views/forget_pasword_view.dart';
import 'package:page_ui/features/auth/presentation/views/login_view.dart';
import 'package:page_ui/features/auth/presentation/views/register_view.dart';
import 'package:page_ui/features/auth/presentation/views/train_view.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/presentation/views/home_view.dart';
import 'package:page_ui/features/intro_screens/presentation/views/developers_view.dart';
import 'package:page_ui/features/intro_screens/presentation/views/landing_view.dart';
import 'package:page_ui/features/intro_screens/presentation/views/not_found_view.dart';
import 'package:page_ui/features/intro_screens/presentation/views/splash_view.dart';
import 'package:page_ui/features/auth/presentation/views/delete_account_verification_view.dart';

sealed class AppRoutes {
  const AppRoutes();

  
  
  
  static const String landingPath = '/';
  static const String splashPath = '/splash';
  static const String developersPath = '/developers';
  static const String loginPath = '/auth/login';
  static const String registerPath = '/auth/register';
  static const String forgetPasswordPath = '/auth/forgot-password';
  static const String emailVerificationPath = '/auth/verify-email';
  static const String homePath = '/app';
  static const String chatPath = '/app/chat/:chatName';
  static const String trainPath = '/onboarding';
  static const String deleteAccountVerificationPath = '/app/delete-account-verify';

  static const _protectedPaths = {homePath, trainPath, deleteAccountVerificationPath};

  static const _authPaths = {
    loginPath,
    registerPath,
    forgetPasswordPath,
    emailVerificationPath,
  };

  
  
  

  static CustomTransitionPage<T> _slideTransition<T>({
    required LocalKey key,
    required Widget child,
    int durationMs = 500,
  }) {
    return CustomTransitionPage<T>(
      key: key,
      transitionDuration: Duration(milliseconds: durationMs),
      child: child,
      transitionsBuilder: (_, animation, __, child) {
        final slide =
            Tween<Offset>(
              begin: const Offset(1.8, 0),
              end: Offset.zero,
            ).animate(
              CurvedAnimation(parent: animation, curve: Curves.easeOutCubic),
            );
        final fade = Tween<double>(begin: 0, end: 1).animate(animation);
        return FadeTransition(
          opacity: fade,
          child: SlideTransition(position: slide, child: child),
        );
      },
    );
  }

  static CustomTransitionPage<T> _fadeTransition<T>({
    required LocalKey key,
    required Widget child,
    int durationMs = 300,
  }) {
    return CustomTransitionPage<T>(
      key: key,
      transitionDuration: Duration(milliseconds: durationMs),
      child: child,
      transitionsBuilder: (_, animation, __, child) {
        return FadeTransition(
          opacity:
              CurvedAnimation(parent: animation, curve: Curves.easeInOut)
                  .drive(Tween<double>(begin: 0, end: 1)),
          child: child,
        );
      },
    );
  }

  static CustomTransitionPage<T> _instantTransition<T>({
    required LocalKey key,
    required Widget child,
  }) {
    return CustomTransitionPage<T>(
      key: key,
      transitionDuration: Duration.zero,
      child: child,
      transitionsBuilder: (_, __, ___, child) => child,
    );
  }

  
  
  
  static final GoRouter router = GoRouter(
    initialLocation: landingPath,

    redirect: (context, state) {
      final location = state.matchedLocation;

      final isProtected = _protectedPaths.contains(location) ||
          location.startsWith('/app/');

      if (!AuthState.isLoggedIn && isProtected) {
        return loginPath;
      }

      if (AuthState.isLoggedIn &&
          (_authPaths.contains(location) || location == splashPath)) {
        return homePath;
      }

      return null;
    },

    errorBuilder: (context, state) => const NotFoundView(),

    routes: <RouteBase>[
      GoRoute(
        path: landingPath,
        name: LandingView.routeName,
        pageBuilder: (_, state) =>
            _fadeTransition(key: state.pageKey, child: const LandingView()),
      ),
      GoRoute(
        path: developersPath,
        name: DevelopersView.routeName,
        pageBuilder: (_, state) =>
            _fadeTransition(key: state.pageKey, child: const DevelopersView()),
      ),

      GoRoute(
        path: splashPath,
        name: SplashView.routeName,
        pageBuilder: (_, state) =>
            _slideTransition(key: state.pageKey, child: const SplashView()),
      ),

      GoRoute(
        path: loginPath,
        name: LoginView.routeName,
        pageBuilder: (_, state) =>
            _slideTransition(key: state.pageKey, child: const LoginView()),
      ),
      GoRoute(
        path: registerPath,
        name: RegisterView.routeName,
        pageBuilder: (_, state) =>
            _slideTransition(key: state.pageKey, child: const RegisterView()),
      ),
      GoRoute(
        path: forgetPasswordPath,
        name: ForgetPaswordView.routeName,
        pageBuilder: (_, state) => _slideTransition(
          key: state.pageKey,
          child: const ForgetPaswordView(),
        ),
      ),
      GoRoute(
        path: emailVerificationPath,
        name: EmailVerficationView.routeName,
        redirect: (context, state) {
          if (state.extra == null) return loginPath;
          return null;
        },
        pageBuilder: (_, state) => _slideTransition(
          key: state.pageKey,
          child: EmailVerficationView(param: state.extra! as LoginParams),
        ),
      ),

      GoRoute(
        path: homePath,
        name: HomeView.routeName,
        pageBuilder: (_, state) =>
            _instantTransition(key: state.pageKey, child: const HomeView()),
        routes: [
          GoRoute(
            path: 'chat/:chatName',
            name: 'chat',
            redirect: (context, state) {
              
              if (state.extra == null) return homePath;
              return null;
            },
            pageBuilder: (_, state) {
              final chat = state.extra! as ChatEntity;
              return _instantTransition(
                key: state.pageKey,
                child: HomeView(initialChat: chat),
              );
            },
          ),
        ],
      ),
      GoRoute(
        path: trainPath,
        name: TrainView.routeName,
        pageBuilder: (_, state) =>
            _instantTransition(key: state.pageKey, child: const TrainView()),
      ),
      GoRoute(
        path: deleteAccountVerificationPath,
        name: 'DeleteAccountVerification',
        pageBuilder: (_, state) =>
            _slideTransition(key: state.pageKey, child: const DeleteAccountVerificationView()),
      ),
    ],
  );

  static void pop<T extends Object?>(BuildContext context, [T? result]) {
    context.pop<T>(result);
  }

  static void goLanding(BuildContext context) =>
      context.goNamed(LandingView.routeName);

  static void goSplash(BuildContext context) =>
      context.goNamed(SplashView.routeName);

  static void pushDevelopersView(BuildContext context) =>
      context.pushNamed(DevelopersView.routeName);

  static void goLogin(BuildContext context) =>
      context.goNamed(LoginView.routeName);

  static void pushRegister(BuildContext context) =>
      context.pushNamed(RegisterView.routeName);

  static void pushForgetPassword(BuildContext context) =>
      context.pushNamed(ForgetPaswordView.routeName);

  static void pushEmailVerification(
    BuildContext context, {
    required LoginParams param,
  }) => context.pushNamed(EmailVerficationView.routeName, extra: param);

  static void goHome(BuildContext context) =>
      context.goNamed(HomeView.routeName);

  static void goChat(BuildContext context, {required ChatEntity chat}) {
    context.goNamed(
      'chat',
      pathParameters: {'chatName': chat.name},
      extra: chat,
    );
  }

  static void goTrain(BuildContext context) =>
      context.goNamed(TrainView.routeName);

  static void pushDeleteAccountVerification(BuildContext context) =>
      context.pushNamed('DeleteAccountVerification');
}
