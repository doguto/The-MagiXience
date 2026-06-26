import * as net from 'net';

const PORT = 7780;
const HOST = '127.0.0.1';
const TIMEOUT_MS = 5000;

/** McpBridgeServer (Unity) にコマンドを送り、JSON レスポンスを返す */
export async function sendCommand(command: Record<string, unknown>): Promise<unknown> {
  return new Promise((resolve, reject) => {
    const client = net.connect(PORT, HOST);
    let buffer = '';
    let settled = false;

    const timer = setTimeout(() => {
      if (!settled) {
        settled = true;
        client.destroy();
        reject(new Error('Unity bridge timeout — ゲームが起動しているか確認してください'));
      }
    }, TIMEOUT_MS);

    client.on('connect', () => {
      client.write(JSON.stringify(command) + '\n');
    });

    client.on('data', (data: Buffer) => {
      buffer += data.toString();
      const nl = buffer.indexOf('\n');
      if (nl !== -1 && !settled) {
        settled = true;
        clearTimeout(timer);
        client.destroy();
        const line = buffer.slice(0, nl).trim();
        try {
          resolve(JSON.parse(line));
        } catch {
          reject(new Error(`Invalid JSON from Unity: ${line}`));
        }
      }
    });

    client.on('error', (err: Error) => {
      if (!settled) {
        settled = true;
        clearTimeout(timer);
        reject(err);
      }
    });
  });
}

/** ゲーム状態スナップショットの型 */
export interface GameState {
  player: {
    hp: number;
    maxHp: number;
    x: number;
    y: number;
    isAlive: boolean;
    isSneaking: boolean;
    isChargeComplete: boolean;
    isInvincible: boolean;
  } | null;
  boss: {
    hp: number;
    maxHp: number;
    isAlive: boolean;
  } | null;
  enemyCount: number;
  screen: {
    minX: number;
    maxX: number;
    minY: number;
    maxY: number;
  };
}
