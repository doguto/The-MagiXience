import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import { sendCommand } from './unity-bridge.js';

const server = new Server(
  { name: 'magixience-mcp', version: '1.0.0' },
  { capabilities: { tools: {} } },
);

// ---- Tool definitions ----

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: [
    {
      name: 'get_game_state',
      description:
        'ゲームの現在状態を取得する。プレイヤーの HP・座標、ボス HP、画面上の敵数など。' +
        '何かアクションを行う前後に呼んで状況を把握すること。',
      inputSchema: { type: 'object' as const, properties: {} },
    },
    {
      name: 'move_player',
      description:
        'プレイヤーを指定方向へ duration_ms ミリ秒間移動させる。' +
        '移動後に stop_move は自動で送信されるので呼ぶ必要はない。',
      inputSchema: {
        type: 'object' as const,
        required: ['x', 'y', 'duration_ms'],
        properties: {
          x: { type: 'number', description: '水平方向 (-1〜1)。右が正。' },
          y: { type: 'number', description: '垂直方向 (-1〜1)。上が正。' },
          duration_ms: { type: 'number', description: '移動時間 (ms)。最大 3000ms。' },
        },
      },
    },
    {
      name: 'shoot',
      description:
        'ノーマルショットを 1 発撃つ。連射クールダウンは Unity 側で制御される。',
      inputSchema: { type: 'object' as const, properties: {} },
    },
    {
      name: 'charge_start',
      description:
        'チャージ開始（スニーク状態に入る）。isChargeComplete が true になったら charge_release を呼ぶこと。',
      inputSchema: { type: 'object' as const, properties: {} },
    },
    {
      name: 'charge_release',
      description:
        'チャージを解放する。isChargeComplete が true の場合は強力なチャージショットが発射される。',
      inputSchema: { type: 'object' as const, properties: {} },
    },
    {
      name: 'ui_navigate',
      description: 'メニュー・モーダル画面でカーソルを移動する。',
      inputSchema: {
        type: 'object' as const,
        required: ['x', 'y'],
        properties: {
          x: { type: 'number', description: '左右 (-1, 0, 1)' },
          y: { type: 'number', description: '上下 (-1, 0, 1)' },
        },
      },
    },
    {
      name: 'ui_submit',
      description: 'UI の決定ボタン（ゲームオーバー後のリトライ選択など）。',
      inputSchema: { type: 'object' as const, properties: {} },
    },
    {
      name: 'ui_cancel',
      description: 'UI のキャンセル／戻るボタン。',
      inputSchema: { type: 'object' as const, properties: {} },
    },
  ],
}));

// ---- Tool handlers ----

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  const a = (args ?? {}) as Record<string, unknown>;

  try {
    switch (name) {
      case 'get_game_state': {
        const state = await sendCommand({ command: 'get_state' });
        return text(JSON.stringify(state, null, 2));
      }

      case 'move_player': {
        const x = Number(a.x ?? 0);
        const y = Number(a.y ?? 0);
        const durationMs = Math.min(Number(a.duration_ms ?? 300), 3000);

        await sendCommand({ command: 'move', x, y });
        await sleep(durationMs);
        await sendCommand({ command: 'stop_move' });

        const state = await sendCommand({ command: 'get_state' });
        return text(JSON.stringify(state, null, 2));
      }

      case 'shoot': {
        await sendCommand({ command: 'attack' });
        return text('{"ok":true}');
      }

      case 'charge_start': {
        await sendCommand({ command: 'charge_start' });
        return text('{"ok":true}');
      }

      case 'charge_release': {
        await sendCommand({ command: 'charge_release' });
        const state = await sendCommand({ command: 'get_state' });
        return text(JSON.stringify(state, null, 2));
      }

      case 'ui_navigate': {
        const x = Number(a.x ?? 0);
        const y = Number(a.y ?? 0);
        await sendCommand({ command: 'ui_navigate', x, y });
        return text('{"ok":true}');
      }

      case 'ui_submit': {
        await sendCommand({ command: 'ui_submit' });
        return text('{"ok":true}');
      }

      case 'ui_cancel': {
        await sendCommand({ command: 'ui_cancel' });
        return text('{"ok":true}');
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    return { content: [{ type: 'text' as const, text: `Error: ${message}` }], isError: true };
  }
});

// ---- Helpers ----

function text(content: string) {
  return { content: [{ type: 'text' as const, text: content }] };
}

function sleep(ms: number): Promise<void> {
  return new Promise((r) => setTimeout(r, ms));
}

// ---- Entry point ----

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
}

main().catch(console.error);
