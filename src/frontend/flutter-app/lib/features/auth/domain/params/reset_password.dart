class ResetPasswordParams {
  final String email;
  final String token;
  final String newPassword;

  ResetPasswordParams({
    required this.email,
    required this.token,
    required this.newPassword,
  });

  Map<String, dynamic> toJson() => {
    "input": {"email": email, "token": token, "newPassword": newPassword},
  };
}
