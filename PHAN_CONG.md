# Phân công — Majesty Gambit (ASP.NET Core MVC)

> Mô hình: **1 project MVC** (Models + Views + Controllers + Services + SignalR).  
> **Không tách FE / BE** — mỗi người làm trọn module từ DB → Service → Controller → Razor View → JS (nếu cần).

Tham chiếu hệ thống hiện tại:
- Backend Java: `chess-backend/`
- Frontend Next.js (chỉ để copy UX/flow): `src/app/`

---

## Thành viên

| Họ tên | MSSV | Vai trò chính |
|--------|------|---------------|
| Nguyễn Văn An | V5250601 | Auth + Layout chung + Home |
| Trần Đức Khang | V5250617 | Lobby + Room + Matchmaking (UI user/Elo trong Lobby) |
| Phạm Ngọc Minh | V5250618 | Game online + Local Chess + History |
| Trần Chí Công | V5250603 | SignalR realtime + Chat + Hạ tầng |

---

## 1. Nguyễn Văn An — V5250601

**Module: Tài khoản & khung ứng dụng**

> FE hiện tại **không có trang Profile riêng** — thông tin user/Elo nằm ở Lobby (Khang). An chỉ cần auth + layout + Home.

| Lớp | Việc cần làm |
|-----|----------------|
| Models / Entities | `User`, `RefreshToken` (nếu vẫn dùng JWT/cookie) |
| Services | Đăng ký, đăng nhập, đăng xuất, hash mật khẩu, Cookie/JWT auth |
| Controllers | `AuthController` (hoặc `AccountController`), `HomeController` |
| Views | `Views/Account/Login.cshtml`, `Register.cshtml`, `Views/Home/Index.cshtml` |
| Shared | `_Layout.cshtml`, nav, CSS brand |

**Map từ Next.js:** `/` → Home · `/login` · `/register`

**API/flow tương đương Java:**
- `POST /api/auth/register` → form Register
- `POST /api/auth/login` → form Login + cookie/session
- `POST /api/auth/logout`
- (tuỳ chọn) endpoint nhỏ trả user hiện tại cho Lobby hiển thị Elo

**Done khi:**
- [ ] Đăng ký / đăng nhập / đăng xuất chạy được
- [ ] Bảo vệ route bằng `[Authorize]`
- [ ] Layout dùng chung cho cả team
- [ ] Home: CTA chơi online / local (link sang Lobby / Chess)

---

## 2. Trần Đức Khang — V5250617

**Module: Phòng chơi & tìm trận**

| Lớp | Việc cần làm |
|-----|----------------|
| Models / Entities | `Room`, `RoomPlayer` + enum `TimeControl`, `RoomStatus` |
| Services | `RoomService` (tạo/join/ready), `MatchmakingService` (queue theo Elo) |
| Controllers | `LobbyController`, `RoomController`, `MatchmakingController` |
| Views | `Views/Lobby/Index.cshtml` (user/Elo + tạo/join phòng + queue), `Views/Room/Index.cshtml` |
| JS | Gọi SignalR join/ready; nút tạo phòng / nhập mã / tìm trận |

**API/flow tương đương Java:**
- `POST /api/rooms` — tạo phòng (mã 6 ký tự)
- `POST /api/rooms/{code}/join`
- `GET /api/rooms/{code}`
- `POST/DELETE /api/matchmaking/queue`
- Ready → khi cả 2 ready thì start game (gọi `GameService`)

**Done khi:**
- [ ] Tạo phòng private + join bằng code
- [ ] Màn lobby: tạo phòng / vào phòng / matchmaking
- [ ] Màn room: thấy 2 player, nút Ready
- [ ] Matchmaking khớp Elo cùng time control → vào game

---

## 3. Phạm Ngọc Minh — V5250618

**Module: Luật cờ, bàn cờ online/local, lịch sử**

| Lớp | Việc cần làm |
|-----|----------------|
| Models / Entities | `Game`, `Move` + enum `GameResult`, `EndReason` |
| Services | `GameService` (validate nước, đồng hồ, resign, timeout, Elo K=20) |
| Controllers | `GameController`, `ChessController`, `HistoryController` |
| Views | `Views/Game/Play.cshtml`, `Views/Chess/Local.cshtml` (offline / vs AI), `Views/History/Index.cshtml` |
| Background | `GameTimeoutService` (hosted service, quét timeout mỗi 1s) |
| Lib | Tích hợp thư viện cờ .NET (vd. ChessDotNet / tương đương chesslib) |

**Map từ Next.js:** `/game/[gameId]` · `/chess` · `/history`

**API/flow tương đương Java:**
- `GET /api/games/{gameId}`, `/active`, `/room/{code}`
- `GET /api/games/history`
- Xử lý move / resign / timeout (qua Service, Hub gọi vào)

**Done khi:**
- [ ] Start game từ room (gán màu, FEN, clock)
- [ ] Nước đi hợp lệ server-side; checkmate / stalemate / resign (≥10 nước)
- [ ] Đồng hồ trừ đúng; timeout kết thúc ván
- [ ] Elo chỉ cập nhật khi `isMatchmaking == true`
- [ ] Trang history phân trang
- [ ] Local chess (`/chess`) chơi được không cần auth (parity FE)

---

## 4. Trần Chí Công — V5250603

**Module: Realtime, chat, DB & chạy project**

| Lớp | Việc cần làm |
|-----|----------------|
| Data | `AppDbContext`, migration, `appsettings` (SQL Server) |
| Hubs | `ChessHub` (SignalR) thay STOMP/WebSocket Java |
| Services glue | Đẩy event: `match_found`, `game_start`, `player_away/back` |
| Views/JS | Chat trên bàn cờ; kết nối SignalR dùng chung |
| Infra | `Program.cs` DI, Swagger (nếu cần), CORS, exception handler, README chạy project |

**Hub methods tương đương Java STOMP:**

| Java | SignalR |
|------|---------|
| `/app/game/{id}/move` | `SendMove` |
| `/app/game/{id}/resign` | `Resign` |
| `/app/game/{id}/timeout` | `CheckTimeout` |
| `/app/game/{id}/reconnect` | `Reconnect` |
| `/app/game/{id}/chat` | `SendChat` |
| `/app/room/{code}/join` | `JoinRoom` |
| `/app/room/{code}/ready` | `SetReady` |
| `/topic/game/{id}` | `Clients.Group(...)` |
| `/user/queue/matchmaking` | `Clients.User(...).MatchFound` |

**Done khi:**
- [ ] DB SQL Server kết nối được (cùng schema ý tưởng với Java)
- [ ] SignalR: move / chat / ready / disconnect → broadcast đúng group
- [ ] Team clone → `dotnet run` chạy được theo README
- [ ] Auth gắn được vào SignalR (cookie hoặc JWT access token)

---

## Phụ thuộc giữa các module (thứ tự làm)

```
Công (DbContext + Program.cs skeleton)
        ↓
An (Auth + Layout)  ←── team dùng layout / login trước
        ↓
Khang (Room + Lobby + Matchmaking) ← cần User đã login
        ↓
Minh (GameService + View bàn cờ) ← cần Room ready
        ↓
Công (SignalR nối Room + Game + Chat) ← tích hợp cuối
```

**Gợi ý sprint:**
1. **Tuần 1:** Công scaffold MVC + DB; An Auth + Layout
2. **Tuần 2:** Khang Lobby/Room; Minh GameService + stub View
3. **Tuần 3:** Công SignalR; Minh bàn cờ + history; tích hợp E2E
4. **Tuần 4:** Fix bug, demo, báo cáo

---

## Quy ước chung (cả nhóm)

1. Nhánh git: `feature/<mssv>-<module>` (vd. `feature/V5250601-auth`)
2. Port mặc định MVC: `http://localhost:5080` (tránh đụng Java `8080` / Next `3000`)
3. Giữ **contract dữ liệu** gần Java (FEN, room code 6 ký tự, Elo default 100, K=20…) để dễ map báo cáo
4. Không commit mật khẩu DB / JWT secret thật — dùng User Secrets / env
5. Mỗi PR chỉ trong phạm vi module mình; interface public ghi rõ trong `Interfaces/`

---

## Checklist tích hợp cuối (cả nhóm)

- [ ] Đăng ký → Login → Lobby
- [ ] Tạo phòng → bạn bè join → Ready → vào bàn cờ
- [ ] Matchmaking → vào bàn cờ → đổi Elo
- [ ] Đánh cờ realtime 2 client
- [ ] Chat trong ván
- [ ] Resign / hết giờ / chiếu hết
- [ ] Xem lịch sử
- [ ] Refresh trang giữa ván vẫn reconnect được
