using Dominodo.E2E.Clients.Common;
using Dominodo.E2E.Clients.Dev;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Core.Faker;
using Dominodo.E2E.Core.Security;

namespace Dominodo.E2E.Clients.Modules.Users;

/// <summary>
/// Builds Users-module request models (fake but valid data by default) and composes full
/// <c>Arrange</c> use cases. Per README §8, any Arrange helper that calls the API throws on
/// non-success — a broken Arrange aborts the test rather than producing a misleading Assert.
/// </summary>
public sealed class UsersRequestBuilder(IUsersClient users, ISqlClient sql, JwtTokenFactory jwtTokenFactory)
    : BaseRequestBuilder
{
    private readonly IUsersClient _users = users;
    private readonly ISqlClient _sql = sql;
    private readonly JwtTokenFactory _jwtTokenFactory = jwtTokenFactory;

    /// <summary>
    /// Builds a valid <see cref="NewUserModel"/> (E.164 phone, unique email, compliant password).
    /// Any field is overridable; use <c>model with { Email = null }</c> to force an absent email.
    /// </summary>
    public NewUserModel BuildNewUserModel(
        string? phone = null,
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        string? password = null)
    {
        return new NewUserModel
        {
            Phone = phone ?? Faker.E164Phone(),
            Email = email ?? $"e2e-{Guid.NewGuid():N}@example.com",
            FirstName = firstName ?? Faker.Name.FirstName(),
            LastName = lastName ?? Faker.Name.LastName(),
            Password = password ?? Faker.StrongPassword(),
        };
    }

    /// <summary>
    /// Full Arrange (parameter overload): builds a valid <see cref="NewUserModel"/> from the given
    /// overrides and registers it. Convenience over <see cref="BuildNewUserModel"/> +
    /// <see cref="RegisterUserAsync(NewUserModel, bool)"/> when you only need to tweak a field or two.
    /// </summary>
    public Task<UserModel> RegisterUserAsync(
        string? phone = null,
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        string? password = null,
        bool activate = true)
    {
        return RegisterUserAsync(BuildNewUserModel(phone, email, firstName, lastName, password), activate);
    }

    /// <summary>
    /// Full Arrange: registers the given user, reads it back via <c>GET /users/{id}</c>, and returns
    /// the persisted <see cref="UserModel"/>. When <paramref name="activate"/> is <c>true</c> (default),
    /// also activates the user via the dev-only SQL endpoint so it is ready to log in.
    /// Throws on any non-success step so a broken Arrange aborts the test immediately.
    /// </summary>
    public async Task<UserModel> RegisterUserAsync(NewUserModel model, bool activate = true)
    {
        var response = await _users.Register(model);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: registering a user returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        if (activate)
        {
            await ForceActivateUserAsync(model.Phone);
        }

        var id = response.Content!.Id;
        var getResponse = await _users.GetById(id);
        if (!getResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: reading back user {id} returned {(int)getResponse.StatusCode}. " +
                $"Body: {getResponse.Error?.Content}");
        }

        return getResponse.Content!;
    }

    /// <summary>
    /// Builds a valid <see cref="UpdateUserModel"/> (non-empty names, unique well-formed email,
    /// short language code). Any field is overridable for negative cases:
    /// <c>model with { FirstName = "" }</c>, or <c>model with { Email = null }</c> to clear it.
    /// Does NOT call the API.
    /// </summary>
    public UpdateUserModel BuildUpdateUserModel(
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? preferredLanguage = null)
    {
        return new UpdateUserModel
        {
            FirstName = firstName ?? Faker.Name.FirstName(),
            LastName = lastName ?? Faker.Name.LastName(),
            Email = email ?? $"e2e-{Guid.NewGuid():N}@example.com",
            PreferredLanguage = preferredLanguage ?? "es",
        };
    }

    /// <summary>
    /// Builds a valid <see cref="NewRoleModel"/> with a unique name.
    /// Any field is overridable: <c>model with { Name = "Custom" }</c>.
    /// </summary>
    public NewRoleModel BuildNewRoleModel(
        string? name = null,
        string? description = null,
        string? scope = null,
        IReadOnlyList<int>? permissionIds = null)
    {
        return new NewRoleModel
        {
            Name = name ?? $"e2e-{Guid.NewGuid():N}",
            Description = description,
            Scope = scope ?? "Platform",
            PermissionIds = permissionIds ?? [1], // "users.manage"
        };
    }

    /// <summary>
    /// Full Arrange (parameter overload): builds a valid <see cref="NewRoleModel"/> from the given
    /// overrides and creates it. Convenience over <see cref="BuildNewRoleModel"/> +
    /// <see cref="CreateRoleAsync(NewRoleModel)"/> when you only need to tweak a field or two.
    /// </summary>
    public Task<RoleDetailModel> CreateRoleAsync(
        string? name = null,
        string? description = null,
        string? scope = null,
        IReadOnlyList<int>? permissionIds = null)
    {
        return CreateRoleAsync(BuildNewRoleModel(name, description, scope, permissionIds));
    }

    /// <summary>
    /// Full Arrange: creates the given role using the seeded <c>roles.manage</c> token, reads it back
    /// via <c>GET /roles/{id}</c> (same token), and returns the persisted <see cref="RoleDetailModel"/>.
    /// Throws on any non-success step so a broken Arrange aborts the test immediately.
    /// </summary>
    public async Task<RoleDetailModel> CreateRoleAsync(NewRoleModel model)
    {
        var token = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        var response = await _users.CreateRole(model, token);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: creating a role returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        var id = response.Content!.Id;
        var getResponse = await _users.GetRoleById(id, token);
        if (!getResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: reading back role {id} returned {(int)getResponse.StatusCode}. " +
                $"Body: {getResponse.Error?.Content}");
        }

        return getResponse.Content!;
    }

    /// <summary>
    /// Builds a valid <see cref="RequestOtpModel"/> for <c>POST /auth/verify/request</c>. By default the
    /// phone is a well-formed E.164 number that belongs to <b>no</b> registered user (drives the 404 case);
    /// pass a registered user's phone for the success case, or an empty/invalid value for the 400 case.
    /// Does NOT call the API.
    /// </summary>
    public RequestOtpModel BuildRequestOtpModel(string? phone = null)
    {
        return new RequestOtpModel(phone ?? Faker.E164Phone());
    }

    // Known OTP plaintext and its BCrypt hash (work factor 11), generated with BCrypt.Net-Next 4.0.3 —
    // the exact hasher src uses (BCryptPasswordHasher). The API persists only the hash and confirms the
    // plaintext via BCrypt.Verify, so seeding this pair lets a test confirm an OTP it "knows" (the real
    // generated code is random and delivered out-of-band, so no endpoint exposes it).
    public const string KnownOtpCode = "123456";
    private const string KnownOtpCodeHash = "$2a$11$ona22C.7tBVZttAJbmIxHOwkSYx1WOLhYmppQUXwooSdKKA4MzXkq";

    /// <summary>
    /// Builds a valid <see cref="ConfirmOtpModel"/> for <c>POST /auth/verify/confirm</c>. Defaults to a
    /// random E.164 phone and <see cref="KnownOtpCode"/>; pass a phone seeded via <see cref="IssueOtpAsync"/>
    /// for the success case. Does NOT call the API.
    /// </summary>
    public ConfirmOtpModel BuildConfirmOtpModel(string? phone = null, string? code = null)
    {
        return new ConfirmOtpModel(phone ?? Faker.E164Phone(), code ?? KnownOtpCode);
    }

    /// <summary>
    /// Full Arrange: seeds an active <c>PhoneVerify</c> OTP for <paramref name="phone"/> straight into
    /// <c>[users].[VerificationCodes]</c> via the dev-only SQL endpoint, using the known plaintext
    /// <see cref="KnownOtpCode"/> (bypassing the random, out-of-band generated code). Returns that plaintext
    /// so the test can confirm it. Throws on non-success.
    /// </summary>
    public async Task<string> IssueOtpAsync(string phone)
    {
        var safePhone = phone.Replace("'", "''");
        var query =
            "INSERT INTO [users].[VerificationCodes] " +
            "(Id, UserId, Phone, Purpose, CodeHash, ExpiresAtUtc, ConsumedAtUtc, Attempts) VALUES " +
            $"(NEWID(), NULL, '{safePhone}', 'PhoneVerify', '{KnownOtpCodeHash}', " +
            "TODATETIMEOFFSET(DATEADD(minute, 30, SYSUTCDATETIME()), 0), NULL, 0)";

        var response = await _sql.Execute(new SqlRequestModel(query));
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: dev SQL issue-OTP returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return KnownOtpCode;
    }

    /// <summary>
    /// Reads a user back via <c>GET /users/{id}</c> and returns the persisted model — used to assert an
    /// endpoint's side effect (e.g. Status flipped to Active). Throws on non-success.
    /// </summary>
    public async Task<UserModel> GetUserAsync(Guid id)
    {
        var response = await _users.GetById(id);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Read-back failed: GET user {id} returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return response.Content!;
    }

    /// <summary>
    /// Builds a valid <see cref="LoginModel"/> using an existing user's credentials.
    /// Any field is overridable: <c>model with { Phone = "+1..." }</c>.
    /// </summary>
    public LoginModel BuildLoginModel(string? phone = null, string? password = null)
    {
        return new LoginModel
        {
            Phone = phone ?? Faker.E164Phone(),
            Password = password ?? Faker.StrongPassword(),
        };
    }

    /// <summary>
    /// Full Arrange: activates a user by phone by writing straight to the DB via the dev-only SQL
    /// endpoint (bypassing OTP). Throws on non-success (the endpoint returns 404 outside Development).
    /// Prefer this over adding "dev" endpoints to the API — the SQL escape hatch keeps test-only
    /// concerns out of <c>src/</c>.
    /// </summary>
    public async Task ForceActivateUserAsync(string phone)
    {
        // Status is persisted as a string (see UserConfiguration). Phones are E.164 fake data, but
        // double any single quote defensively so the inline literal is always well-formed.
        var safePhone = phone.Replace("'", "''");
        var query = $"UPDATE [users].[Users] SET [Status] = 'Active' WHERE [Phone] = '{safePhone}'";

        var response = await _sql.Execute(new SqlRequestModel(query));
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: dev SQL activate returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }
    }

    /// <summary>
    /// Full Arrange: registers a brand-new user, activates it via the dev-only SQL endpoint
    /// (bypassing OTP — only available in Development), and logs in.
    /// Returns the issued tokens and the model so callers have the phone + password for follow-up calls.
    /// Throws on any non-success step.
    /// </summary>
    public async Task<(AuthTokensModel Tokens, NewUserModel User)> CreateUserAndAuthenticateAsync(
        string? phone = null,
        string? email = null,
        string? password = null)
    {
        var newUser = BuildNewUserModel(phone: phone, email: email, password: password);
        await RegisterUserAsync(newUser, activate: true);

        var loginModel = new LoginModel { Phone = newUser.Phone, Password = newUser.Password };
        var response = await _users.Login(loginModel);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: login returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return (response.Content!, newUser);
    }

    /// <summary>
    /// Builds a valid <see cref="InviteMemberModel"/> (E.164 phone, positive role id). Any field is
    /// overridable: <c>model with { Phone = "" }</c> for the 400 case. Does NOT call the API — the
    /// phone/role it references need not exist (used as-is for validation/negative cases).
    /// </summary>
    public InviteMemberModel BuildInviteMemberModel(string? phone = null, int roleId = 1)
    {
        return new InviteMemberModel
        {
            Phone = phone ?? Faker.E164Phone(),
            RoleId = roleId,
        };
    }

    /// <summary>
    /// Full Arrange: registers a fresh activated user and creates a Tenant-scope role, then returns a
    /// valid <see cref="InviteMemberModel"/> that would invite that user with that role. Does NOT call
    /// the invite endpoint (the endpoint under test). Throws on any non-success prerequisite step.
    /// </summary>
    public async Task<InviteMemberModel> ArrangeInviteMemberModelAsync()
    {
        var user = await RegisterUserAsync();
        var role = await CreateRoleAsync(scope: "Tenant");
        return BuildInviteMemberModel(phone: user.Phone, roleId: role.Id);
    }

    /// <summary>
    /// Full Arrange: registers a fresh activated user, creates a Tenant-scope role, and invites the user
    /// into <paramref name="tenantSlug"/> as a platform manager (memberships.manage) — leaving an
    /// <c>Invited</c> membership. Returns the created membership id (only the invite endpoint exposes it —
    /// the memberships list does not), the persisted user, and the granted role id. Throws on any
    /// non-success step.
    /// </summary>
    public async Task<ArrangedMembership> InvitePlatformMembershipAsync(string tenantSlug)
    {
        var user = await RegisterUserAsync();
        var role = await CreateRoleAsync(scope: "Tenant");

        var token = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);
        var model = new InviteMemberModel { Phone = user.Phone, RoleId = role.Id };

        var response = await _users.InviteMember(model, tenantSlug, token);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: inviting a member into '{tenantSlug}' returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return new ArrangedMembership(response.Content!.Id, user, role.Id);
    }

    /// <summary>
    /// Builds a valid <see cref="ChangeMemberRoleModel"/>. Does NOT call the API.
    /// </summary>
    public ChangeMemberRoleModel BuildChangeMemberRoleModel(int roleId)
    {
        return new ChangeMemberRoleModel { RoleId = roleId };
    }

    /// <summary>
    /// Full Arrange: invites a fresh user into <paramref name="tenantSlug"/> (via the platform manager
    /// path) and then accepts on the invitee's behalf, leaving an <c>Active</c> membership. Returns the
    /// same <see cref="ArrangedMembership"/> (its id is stable across accept). Throws on any non-success
    /// step. Use for endpoints that require an already-active membership (suspend, …) — never suspend a
    /// seeded membership, which other tests depend on.
    /// </summary>
    public async Task<ArrangedMembership> ArrangeActiveMembershipAsync(string tenantSlug)
    {
        var invited = await InvitePlatformMembershipAsync(tenantSlug);

        var token = _jwtTokenFactory.CreateUserToken(invited.User.Id);
        var response = await _users.AcceptInvitation(tenantSlug, token);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: accepting invitation in '{tenantSlug}' returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return invited;
    }

    /// <summary>
    /// Full Arrange: builds an <c>Active</c> membership (invite + accept) and then suspends it via the
    /// platform manager, leaving a <c>Suspended</c> membership. Returns the same
    /// <see cref="ArrangedMembership"/> (its id is stable). Throws on any non-success step. Use for
    /// endpoints that require a suspended membership (reactivate).
    /// </summary>
    public async Task<ArrangedMembership> ArrangeSuspendedMembershipAsync(string tenantSlug)
    {
        var active = await ArrangeActiveMembershipAsync(tenantSlug);

        var token = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);
        var response = await _users.SuspendMembership(active.MembershipId, tenantSlug, token);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: suspending membership {active.MembershipId} in '{tenantSlug}' returned " +
                $"{(int)response.StatusCode}. Body: {response.Error?.Content}");
        }

        return active;
    }

    /// <summary>
    /// Reads back a single membership via <c>GET /memberships</c> (with the seeded memberships.manage
    /// token, scoped to <paramref name="tenantSlug"/>) and returns the row for <paramref name="userId"/>,
    /// or <c>null</c> if absent — used to assert an endpoint's side effect (e.g. Status flipped to Active).
    /// Throws on a non-success read.
    /// </summary>
    public async Task<MembershipModel?> FindMembershipAsync(string tenantSlug, Guid userId)
    {
        var token = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);

        var response = await _users.GetMemberships(1, 100, tenantSlug, token);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Read-back failed: GET memberships for '{tenantSlug}' returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return response.Content!.Items.FirstOrDefault(m => m.UserId == userId);
    }

    /// <summary>
    /// Builds a valid <see cref="UpdateRoleModel"/> with a unique name.
    /// Any field is overridable: <c>model with { Name = "Custom" }</c>.
    /// </summary>
    public UpdateRoleModel BuildUpdateRoleModel(
        string? name = null,
        string? description = null,
        IReadOnlyList<int>? permissionIds = null)
    {
        return new UpdateRoleModel
        {
            Name = name ?? $"e2e-{Guid.NewGuid():N}",
            Description = description,
            PermissionIds = permissionIds ?? [1], // "users.manage"
        };
    }
}
