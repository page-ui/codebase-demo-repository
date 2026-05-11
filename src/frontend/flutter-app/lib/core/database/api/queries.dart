class Queries {
  
  static String loginMutation = r'''
    mutation Login($input: LoginInput!) {
      login(input: $input) {
        accessToken
        refreshToken
      }
    }
  ''';

  static String registerMutation = r'''
    mutation Register($input: RegisterInput!) {
      register(input: $input)
    }
  ''';

  static String forgotPasswordRequestMutation = r'''
    mutation ForgotPassword($email: String!) {
      forgotPasswordRequest(email: $email)
    }
  ''';

  static String verifyResetCodeMutation = r'''
    mutation VerifyCode($email: String!, $code: String!) {
      verifyResetCode(email: $email, code: $code)
    }
  ''';

  static String resetPasswordMutation = r'''
    mutation ResetPassword($input: ResetPasswordInput!) {
      resetPassword(input: $input)
    }
  ''';

  static String refreshTokenMutation = r'''
    mutation RefreshToken($refreshToken: String!) {
      refreshToken(refreshToken: $refreshToken) {
        accessToken
        refreshToken
      }
    }
  ''';

  static String emailVerficationMutation = r'''
    mutation VerifyEmail($email: String!, $code: String!) {
      verifyEmail(email: $email, code: $code)
    }
  ''';

  static String resendVerificationMutation = r'''
    mutation ResendVerification($email: String!) {
      resendVerification(email: $email)
    }
  ''';

  static String signOutMutation = r'''
    mutation SignOut($refreshToken: String!) {
      signOut(refreshToken: $refreshToken)
    }
  ''';

  static String deleteAccountMutation = r'''
    mutation DeleteAccount($code: String!) {
      deleteAccount(code: $code)
    }
  ''';

  

  static String createChatMutation = r'''
    mutation CreateChat($input: CreateChatInput!) {
      createChat(input: $input) {
        chat {
          chatKey
          name
          modelId
          createdAt
        }
      }
    }
  ''';

  static String sendMessageMutation = r'''
    mutation CreateMessage($input: CreateMessageInput!) {
      createMessage(input: $input) {
        title
      }
    }
  ''';

  static String chatRoomsQuery = r'''
    query GetChats($first: Int, $after: String) {
      chats(
        first: $first
        after: $after
        order: [{ createdAt: DESC }]
      ) {
        pageInfo {
          hasNextPage
          endCursor
        }
        edges {
          node {
            chatKey
            name
            createdAt
          }
        }
      }
    }
  ''';

  static String searchChatQuery = r'''
    query SearchChats($name: String!, $first: Int, $after: String,) {
      searchChats(name: $name, first: $first, after: $after, order: [{ createdAt: DESC }]) {
        pageInfo {
          hasNextPage
          endCursor
        }
        edges {
          node {
            chatKey
            name
            modelId
            createdAt
          }
        }
      }
    }
  ''';

  static String getMessagesQuery = r'''
    query GetMessages($chatKey: String!, $first: Int, $after: String, $order: [MessageViewSortInput!]) {
      messages(
        chatKey: $chatKey
        first: $first
        after: $after
        order: $order
        where: { type: { nin: [THINKING] } }
      ) {
        totalCount
        pageInfo {
          hasNextPage
          endCursor
        }
        edges {
          node {
            messageKey
            chatKey
            title
            content
            isQuestion
            type
            status
            createdAt
            updatedAt
            replyToKey
            attachmentUrl
            senderType
          }
        }
      }
    }
  ''';

  static String requestDeleteMutation = r'''
    mutation RequestAccountDeletion {
      requestAccountDeletion
    }
  ''';

  static String deleteChatMutation = r'''
    mutation DeleteChat($chatKey: String!) {
      deleteChat(chatKey: $chatKey)
    }
  ''';

  static String renameChatMutation = r'''
    mutation RenameChat($input: RenameChatInput!) {
      renameChat(input: $input) {
        chatKey
        name
        modelId
        createdAt
        updatedAt
      }
    }

    
  ''';

  static String onMessageCreatedSubscription = r'''
    subscription OnMessageCreated($chatKey: String!) {
      onMessageCreated(chatKey: $chatKey) {
        messageKey
        chatKey
        title
        content
        isQuestion
        type
        status
        createdAt
        updatedAt
        replyToKey
        attachmentUrl
        senderType
      }
    }
  ''';

  
  
  
  
  
}
