ROWS = 6
COLS = 7

RED    = "\033[91m"
YELLOW = "\033[93m"
DIM    = "\033[2m"
BOLD   = "\033[1m"
RESET  = "\033[0m"

COLORS = {1: RED, 2: YELLOW}
NAMES  = {1: f"{RED}Player 1{RESET}", 2: f"{YELLOW}Player 2{RESET}"}


def make_board():
    return [[0] * COLS for _ in range(ROWS)]


def print_board(board):
    print()
    header = "  " + "  ".join(f"{BOLD}{c+1}{RESET}" for c in range(COLS))
    print(header)
    print("  " + "---" * COLS)
    for row in board:
        cells = []
        for cell in row:
            if cell == 0:
                cells.append(f"{DIM}·{RESET}")
            else:
                cells.append(f"{COLORS[cell]}●{RESET}")
        print("| " + "  ".join(cells) + " |")
    print("  " + "---" * COLS)
    print()


def drop_piece(board, col, player):
    for row in range(ROWS - 1, -1, -1):
        if board[row][col] == 0:
            board[row][col] = player
            return row
    return -1


def check_win(board, row, col, player):
    directions = [(0, 1), (1, 0), (1, 1), (1, -1)]
    for dr, dc in directions:
        count = 1
        for sign in (1, -1):
            r, c = row + dr * sign, col + dc * sign
            while 0 <= r < ROWS and 0 <= c < COLS and board[r][c] == player:
                count += 1
                r += dr * sign
                c += dc * sign
        if count >= 4:
            return True
    return False


def is_draw(board):
    return all(board[0][c] != 0 for c in range(COLS))


def main():
    board = make_board()
    player = 1

    print(f"\n{BOLD}=== Connect Four ==={RESET}")
    print(f"{NAMES[1]} vs {NAMES[2]}\n")

    while True:
        print_board(board)
        print(f"{NAMES[player]}'s turn — choose a column (1–{COLS}): ", end="")

        try:
            col = int(input()) - 1
        except (ValueError, EOFError):
            print("  Please enter a number.\n")
            continue

        if not (0 <= col < COLS):
            print(f"  Column must be between 1 and {COLS}.\n")
            continue

        row = drop_piece(board, col, player)
        if row == -1:
            print("  That column is full. Pick another.\n")
            continue

        if check_win(board, row, col, player):
            print_board(board)
            print(f"{NAMES[player]} wins! Congratulations!\n")
            break

        if is_draw(board):
            print_board(board)
            print("It's a draw!\n")
            break

        player = 2 if player == 1 else 1


if __name__ == "__main__":
    main()
