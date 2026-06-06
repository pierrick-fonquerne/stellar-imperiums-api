# Design — Authentification JWT (login, refresh, logout, me)

Date : 2026-06-06
Statut : validé section par section, en attente de relecture finale
Périmètre : API uniquement (le frontend consommera ces endpoints dans un milestone ultérieur)

## 1. Contexte et objectif

L'inscription (`POST /api/v1/auth/register`) existe avec un domaine `User` riche
(`Username`, `Email`, `PasswordHash` Argon2id, `Role`, `IsSuspended`,
`MustChangePassword`, `RecordLogin()`). Ce milestone complète le cycle
d'authentification : connexion par **email + mot de passe** (exigence de
l'énoncé, §Connexion), session maintenue par access JWT court + refresh token
révocable, déconnexion effective, et premier endpoint protégé.

## 2. Décisions structurantes

| Décision | Choix | Justification |
|---|---|---|
| Architecture tokens | Access JWT 15 min + refresh opaque 30 j avec rotation | Sessions de jeu longues, révocation immédiate (suspension) |
| Transport refresh | Cookie `HttpOnly` + `Secure` + `SameSite=Strict` + `Path=/api/v1/auth` | Immunisé XSS ; l'access reste en mémoire JS côté front |
| Signature JWT | HS256, clé symétrique ≥ 32 octets | Émetteur = validateur unique (monolithe) |
| Modélisation refresh | Entité `RefreshToken` Rich Domain | Rotation/révocation = invariants métier, testables sans DB |
| Hash du refresh en DB | SHA-256 | Secret aléatoire 256 bits : un hash rapide suffit (Argon2id réservé aux mots de passe à faible entropie) |
| Anti brute-force | Rate limiter ASP.NET : 5 tentatives/min/IP sur `/auth/login` | Recommandation CNIL/OWASP |
| Tests d'intégration | Inclus dans ce milestone (Testcontainers PostgreSQL) | Le pipeline JwtBearer + cookies ne se valide pas unitairement |

## 3. Architecture par couche

### 3.1 Domain — `Domain/Tokens/`

- `RefreshToken` (entité) : `UserId`, `TokenHash` (SHA-256, jamais le token en
  clair), `FamilyId` (Guid, détection de réutilisation), `ExpiresAt`,
  `CreatedAt`, `RevokedAt?`, `ReplacedByTokenHash?`.
  - `Create(userId, tokenHash, familyId, expiresAt)` : expiration strictement
    future, hash non vide.
  - `Rotate(newTokenHash, expiresAt)` : retourne le successeur (même famille),
    marque l'actuel remplacé ; lève une exception si révoqué ou déjà roté.
  - `Revoke(when)` : idempotent.
  - `IsActive` : ni révoqué, ni roté, ni expiré.
- Exceptions : `RefreshTokenExpiredException`, `RefreshTokenRevokedException`,
  `RefreshTokenReuseException`.

### 3.2 Application

- Abstractions :
  - `ITokenService` : `CreateAccessToken(User)` → `(token, expiresIn)` ;
    `GenerateRefreshToken()` → `(valeurOpaque, hash)`.
  - `IRefreshTokenRepository` : `GetByHashAsync`, `AddAsync`,
    `RevokeFamilyAsync(familyId)`, `SaveChangesAsync`.
  - `IUserRepository` (extension) : `GetByEmailAsync(Email)`, `GetByIdAsync(Guid)`.
- Commands Wolverine (pattern `RegisterUser` existant) :
  - `LoginUserCommand(Email, Password)` + handler + validator FluentValidation.
  - `RefreshTokenCommand(RefreshTokenValue)` + handler.
  - `LogoutCommand(RefreshTokenValue)` + handler.
- Exceptions : `InvalidCredentialsException` (générique : ne révèle jamais si
  l'email ou le mot de passe est en cause), `UserSuspendedException`.

### 3.3 Infrastructure

- `JwtTokenService` : HS256 via `Microsoft.IdentityModel.JsonWebTokens` ;
  refresh = 32 octets `RandomNumberGenerator` encodés Base64Url, hashés SHA-256.
- `JwtOptions` (options pattern) : `Issuer`, `Audience`,
  `AccessTokenLifetimeMinutes`, `RefreshTokenLifetimeDays`, `SigningKey`.
  `ValidateOnStart()` : démarrage refusé si clé < 32 octets. Clé fournie par
  user-secrets (dev) ou variable d'environnement (prod), jamais dans appsettings.
- `EfRefreshTokenRepository`, `RefreshTokenConfiguration` (index unique sur
  `TokenHash`, index sur `FamilyId`, FK `UserId` cascade), migration
  `AddRefreshTokens`.

### 3.4 Api

- `AuthController` (existant, étendu) :
  - `POST /api/v1/auth/login` → body `{accessToken, expiresIn, tokenType,
    user{id, username, email, role}, mustChangePassword}` + `Set-Cookie` refresh.
  - `POST /api/v1/auth/refresh` → body `{accessToken, expiresIn, tokenType}`
    (sans `user` ni `mustChangePassword`) + rotation du cookie.
  - `POST /api/v1/auth/logout` → `204` + suppression du cookie.
- `UsersController` (nouveau) : `GET /api/v1/users/me` `[Authorize]` →
  `MeResponse(id, username, email, role, registrationDate, lastLoginAt)`.
- `Program.cs` : `AddAuthentication().AddJwtBearer()` (ClockSkew 30 s),
  `AddAuthorization()`, `AddRateLimiter` (fixed window 5/min/IP, policy
  `auth-login`), `UseAuthentication()` avant `UseAuthorization()`.
- Contracts : `LoginRequest`, `LoginResponse`, `RefreshResponse`, `MeResponse`.

## 4. Flux et codes d'erreur

### POST /auth/login

1. Validation (email format, password requis) → `400 ValidationProblemDetails`.
2. `GetByEmailAsync` absent → exécuter quand même `Verify()` contre un hash
   factice (anti-timing) → `401` générique.
3. `Verify()` faux → `401` générique.
4. `IsSuspended` → `403` (`account_suspended`).
5. Succès : `RecordLogin(UtcNow)`, refresh token créé (nouvelle famille) →
   `200` + cookie. `mustChangePassword` est un flag informatif : le login
   passe (l'énoncé impose le changement à la première connexion après mot de
   passe temporaire ; l'écran de changement arrivera avec le milestone reset).

### POST /auth/refresh

1. Cookie absent / hash inconnu / expiré → `401`.
2. Token déjà roté (`ReplacedByTokenHash` non null) = réutilisation →
   révocation de toute la famille → `401`.
3. Token révoqué → `401`.
4. User suspendu → `403` + révocation de la famille.
5. Sinon `Rotate()` + nouveau JWT → `200` + nouveau cookie (30 j glissants).

### POST /auth/logout

Cookie présent : révoquer la famille, effacer le cookie → `204`.
Cookie absent : `204` (idempotent).

### GET /users/me

`[Authorize]` → claim `sub` → `GetByIdAsync` → `200`. Introuvable → `401`.

### Transverse

- Mapping exceptions → `ProblemDetails` ajouté au `DomainExceptionMiddleware`.
- Rate limiting dépassé → `429`.

## 5. Sécurité

### Claims (minimisation)

`sub` (Guid user), `unique_name` (username), `role`, `jti`, `iss`, `aud`,
`iat`, `exp`. **Pas d'email** : un JWT est signé, pas chiffré — toute donnée
embarquée est lisible. `/users/me` fournit l'email de façon contrôlée.

### Cookie

`refresh_token` : `HttpOnly`, `Secure`, `SameSite=Strict`,
`Path=/api/v1/auth`, `Max-Age=30 j`.

### Contrainte d'hébergement

`SameSite=Strict` impose front et API sur le même site. Dev : proxy Vite
(`localhost` → `localhost`). Prod : reverse proxy ou sous-domaines du même
domaine enregistrable. À documenter dans le README du frontend au milestone
suivant.

## 6. Stratégie de tests

### Domain.Tests — `RefreshTokenTests` (~15)

`Create` (invariants), `Rotate` (succès même famille ; révoqué → exception ;
déjà roté → exception), `Revoke` (idempotence), états `IsActive`/`IsExpired`.

### Application.Tests (~25)

- `LoginUserHandlerTests` : succès (RecordLogin + token créé), email inconnu →
  `InvalidCredentialsException` **avec** `Verify()` appelé malgré tout (preuve
  anti-timing), mauvais mot de passe, suspendu, flag `mustChangePassword`.
- `RefreshTokenHandlerTests` : rotation, réutilisation → famille révoquée,
  expiré, révoqué, suspendu.
- `LogoutHandlerTests` : révocation famille, idempotence.
- `LoginUserValidatorTests`.

### Infrastructure.Tests — `JwtTokenServiceTests` (~10)

Claims attendus présents et email absent, expiration, signature vérifiable,
refresh : 32 octets d'entropie, hash déterministe.

### Api.IntegrationTests (Testcontainers PostgreSQL)

- Fixture xUnit : conteneur PostgreSQL partagé par collection, migrations
  appliquées au démarrage, `WebApplicationFactory<Program>`.
- Scénarios : parcours nominal `register → login → me → refresh → logout` ;
  login mauvais mot de passe → `401` ; `me` sans token → `401` ; refresh après
  logout → `401` ; réutilisation d'un refresh roté → `401` + famille morte ;
  rate limiting → `429` à la 6ᵉ tentative.

Implémentation en TDD : chaque test écrit avant le code correspondant.

## 7. Hors périmètre

- Reset de mot de passe par email (milestone dédié — le domaine est déjà prêt :
  `StartPasswordReset`, `MustChangePassword`).
- Endpoint de changement de mot de passe.
- Pages frontend (login UI consommera ces endpoints au milestone suivant).
- Purge planifiée des refresh tokens expirés en DB (chore ultérieure).
