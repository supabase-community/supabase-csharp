using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue.Mfa;
using static GotrueTests.TestUtils;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using static Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert;
using static Supabase.Gotrue.Constants.AuthState;

namespace GotrueTests;

[TestClass]
[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
public class MfaClientTests
{
    private IGotrueClient<User, Session> _client;
    private IGotrueAdminClient<User> _adminClient;

    private readonly string _serviceKey = GenerateServiceRoleToken();

    [TestInitialize]
    public void TestInitializer()
    {
        _client = new Client(
            new ClientOptions
            {
                AllowUnconfirmedUserSessions = true,
                Url = "http://127.0.0.1:54321/auth/v1",
            }
        );
        _adminClient = new AdminClient(
            _serviceKey,
            new ClientOptions
            {
                AllowUnconfirmedUserSessions = true,
                Url = "http://127.0.0.1:54321/auth/v1",
            }
        );
    }

    [TestMethod("MFA: Complete flow")]
    public async Task MfaFlow()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUp(email, PASSWORD);
        VerifyGoodSession(session);

        var mfaEnrollParams = new MfaEnrollParams
        {
            Issuer = "Supabase",
            FactorType = "totp",
            FriendlyName = "Enroll test",
        };

        var enrollResponse = await _client.Enroll(mfaEnrollParams);
        IsNotNull(enrollResponse.Id);
        AreEqual(mfaEnrollParams.FriendlyName, enrollResponse.FriendlyName);
        AreEqual(mfaEnrollParams.FactorType, enrollResponse.Type);

        var challengeResponse = await _client.Challenge(
            new MfaChallengeParams { FactorId = enrollResponse.Id }
        );
        IsNotNull(challengeResponse.Id);

        string totpCode = TotpGenerator.GeneratePin(enrollResponse.Totp.Secret, 30, 6);
        var verifyResponse = await _client.Verify(
            new MfaVerifyParams
            {
                FactorId = enrollResponse.Id,
                ChallengeId = challengeResponse.Id,
                Code = totpCode,
            }
        );
        IsNotNull(verifyResponse);
        VerifyGoodSession(verifyResponse);

        await _client.SignOut();

        session = await _client.SignIn(email, PASSWORD);
        VerifyGoodSession(session);

        var assuranceLevel = await _client.GetAuthenticatorAssuranceLevel();
        AreEqual(AuthenticatorAssuranceLevel.aal1, assuranceLevel.CurrentLevel);
        AreEqual(AuthenticatorAssuranceLevel.aal2, assuranceLevel.NextLevel);

        totpCode = TotpGenerator.GeneratePin(enrollResponse.Totp.Secret, 30, 6);
        await _client.ChallengeAndVerify(
            new MfaChallengeAndVerifyParams { FactorId = enrollResponse.Id, Code = totpCode }
        );

        assuranceLevel = await _client.GetAuthenticatorAssuranceLevel();
        AreEqual(AuthenticatorAssuranceLevel.aal2, assuranceLevel.CurrentLevel);
        AreEqual(AuthenticatorAssuranceLevel.aal2, assuranceLevel.NextLevel);

        var factors = await _client.ListFactors();
        IsTrue(factors.Totp.Count == 1);

        var unenrollResponse = await _client.Unenroll(
            new MfaUnenrollParams { FactorId = enrollResponse.Id }
        );
        IsNotNull(unenrollResponse);

        await _client.SignOut();

        session = await _client.SignIn(email, PASSWORD);
        VerifyGoodSession(session);

        assuranceLevel = await _client.GetAuthenticatorAssuranceLevel();
        AreEqual(AuthenticatorAssuranceLevel.aal1, assuranceLevel.CurrentLevel);
        AreEqual(AuthenticatorAssuranceLevel.aal1, assuranceLevel.NextLevel);

        factors = await _client.ListFactors();
        IsTrue(factors.Totp.Count == 0);
    }

    [TestMethod("MFA Admin: List factors for user")]
    public async Task MfaAdminListFactorsForUser()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUp(email, PASSWORD);
        VerifyGoodSession(session);

        var mfaEnrollParams = new MfaEnrollParams
        {
            Issuer = "Supabase",
            FactorType = "totp",
            FriendlyName = "Enroll test",
        };

        var enrollResponse = await _client.Enroll(mfaEnrollParams);
        IsNotNull(enrollResponse.Id);

        var factors = await _adminClient.ListFactors(
            new MfaAdminListFactorsParams { UserId = session.User.Id }
        );
        IsNotNull(factors);
        AreEqual(1, factors.Factors.Count);
        AreEqual(enrollResponse.Id, factors.Factors.FirstOrDefault().Id);
        AreEqual("unverified", factors.Factors.FirstOrDefault().Status);

        var totpCode = TotpGenerator.GeneratePin(enrollResponse.Totp.Secret, 30, 6);
        await _client.ChallengeAndVerify(
            new MfaChallengeAndVerifyParams { FactorId = enrollResponse.Id, Code = totpCode }
        );

        factors = await _adminClient.ListFactors(
            new MfaAdminListFactorsParams { UserId = session.User.Id }
        );
        IsNotNull(factors);
        AreEqual(1, factors.Factors.Count);
        AreEqual(enrollResponse.Id, factors.Factors.FirstOrDefault().Id);
        AreEqual("verified", factors.Factors.FirstOrDefault().Status);
    }

    [TestMethod("MFA Admin: Delete factor for user")]
    public async Task MfaAdminDeleteFactorForUser()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUp(email, PASSWORD);
        VerifyGoodSession(session);

        var mfaEnrollParams = new MfaEnrollParams
        {
            Issuer = "Supabase",
            FactorType = "totp",
            FriendlyName = "Enroll test",
        };

        var enrollResponse = await _client.Enroll(mfaEnrollParams);
        IsNotNull(enrollResponse.Id);

        var listFactors = await _adminClient.ListFactors(
            new MfaAdminListFactorsParams { UserId = session.User.Id }
        );
        AreEqual(1, listFactors.Factors.Count);

        var deleteFactorResponse = await _adminClient.DeleteFactor(
            new MfaAdminDeleteFactorParams { Id = enrollResponse.Id, UserId = session.User.Id }
        );
        AreEqual(enrollResponse.Id, deleteFactorResponse.Id);

        listFactors = await _adminClient.ListFactors(
            new MfaAdminListFactorsParams { UserId = session.User.Id }
        );
        AreEqual(0, listFactors.Factors.Count);
    }

    [TestMethod("MFA: Invalid TOTP")]
    public async Task MfaInvalidTotp()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUp(email, PASSWORD);
        VerifyGoodSession(session);

        var mfaEnrollParams = new MfaEnrollParams
        {
            Issuer = "Supabase",
            FactorType = "totp",
            FriendlyName = "Enroll test",
        };

        var enrollResponse = await _client.Enroll(mfaEnrollParams);
        IsNotNull(enrollResponse.Id);

        await ThrowsExceptionAsync<GotrueException>(async () =>
        {
            await _client.ChallengeAndVerify(
                new MfaChallengeAndVerifyParams { FactorId = enrollResponse.Id, Code = "12345" }
            );
        });
    }

    [TestMethod("MFA: Invalid TOTP type during Enroll")]
    public async Task MfaInvalidTotpTypeDuringEnroll()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUp(email, PASSWORD);
        VerifyGoodSession(session);

        var mfaEnrollParams = new MfaEnrollParams
        {
            Issuer = "Supabase",
            FactorType = "InvalidType",
            FriendlyName = "Enroll test",
        };

        await ThrowsExceptionAsync<GotrueException>(async () =>
        {
            await _client.Enroll(mfaEnrollParams);
        });
    }

    [TestMethod("MFA: Invalid FactorId during Unenroll")]
    public async Task MfaInvalidFactorIdDuringUnenroll()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUp(email, PASSWORD);
        VerifyGoodSession(session);

        await ThrowsExceptionAsync<GotrueException>(async () =>
        {
            await _client.Unenroll(new MfaUnenrollParams { FactorId = "" });
        });
    }

    [TestMethod("MFA: Invalid FactorId during Challenge")]
    public async Task MfaInvalidFactorIdDuringChallenge()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUp(email, PASSWORD);
        VerifyGoodSession(session);

        await ThrowsExceptionAsync<GotrueException>(async () =>
        {
            await _client.Challenge(new MfaChallengeParams { FactorId = "" });
        });
    }

    [TestMethod("MFA: Invalid ChallengeId during Verify")]
    public async Task MfaInvalidChallengeIdDuringVerify()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUp(email, PASSWORD);
        VerifyGoodSession(session);

        var mfaEnrollParams = new MfaEnrollParams
        {
            Issuer = "Supabase",
            FactorType = "totp",
            FriendlyName = "Enroll test",
        };

        var enrollResponse = await _client.Enroll(mfaEnrollParams);
        IsNotNull(enrollResponse.Id);

        var challengeResponse = await _client.Challenge(
            new MfaChallengeParams { FactorId = enrollResponse.Id }
        );
        IsNotNull(challengeResponse.Id);

        await ThrowsExceptionAsync<GotrueException>(async () =>
        {
            await _client.Verify(
                new MfaVerifyParams
                {
                    Code = "",
                    ChallengeId = "",
                    FactorId = enrollResponse.Id,
                }
            );
        });
    }

    [TestMethod("MFA: Invalid FactorId during Verify")]
    public async Task MfaInvalidFactorIdDuringVerify()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUp(email, PASSWORD);
        VerifyGoodSession(session);

        var mfaEnrollParams = new MfaEnrollParams
        {
            Issuer = "Supabase",
            FactorType = "totp",
            FriendlyName = "Enroll test",
        };

        var enrollResponse = await _client.Enroll(mfaEnrollParams);
        IsNotNull(enrollResponse.Id);

        var challengeResponse = await _client.Challenge(
            new MfaChallengeParams { FactorId = enrollResponse.Id }
        );
        IsNotNull(challengeResponse.Id);

        await ThrowsExceptionAsync<GotrueException>(async () =>
        {
            await _client.Verify(
                new MfaVerifyParams
                {
                    Code = "",
                    ChallengeId = challengeResponse.Id,
                    FactorId = "",
                }
            );
        });
    }

    private void VerifyGoodSession(Session session)
    {
        AreEqual(_client.CurrentUser.Id, session.User.Id);
        IsNotNull(session.AccessToken);
        IsNotNull(session.RefreshToken);
        IsNotNull(session.User);
    }
}

