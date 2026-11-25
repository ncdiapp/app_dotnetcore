/**
 * Operation Type taxonomy for Data Model Operation editor.
 * Categories are UI-only (derived from ActionType); not persisted.
 *
 * Spec: Document/AgentDesign/AppConfigurator-Commands-Knowledge-Plan.md
 */

import {
  EmTransactionOrganizedType,
  getTransactionCommandActionTypeDisplay,
  type TransactionCommandActionTypeOption,
} from './transactionCommandActionTypes';

/** Not offered in new Operation Type picks (existing rows may still display by Id). */
export const COMMAND_ACTION_TYPE_PICKER_EXCLUDED_IDS = new Set<number>([
  51, // UI: Open Form Creation Window
  55, // UI: Open Form Edit Window
  70, // Create User
]);

export type CommandTypeTaxonomyContext = 'masterDetail' | 'listEdit' | 'workflowEmbedded';

export type CommandTypeLeaf = {
  kind: 'type';
  actionTypeId: number;
  display: string;
};

export type CommandTypeGroup = {
  kind: 'group';
  id: string;
  label: string;
  children: CommandTypeLeaf[];
};

export type CommandTypeCategory = {
  kind: 'category';
  id: string;
  label: string;
  children: Array<CommandTypeGroup | CommandTypeLeaf | CommandTypeCategory>;
  /** When set, selecting the category binds this ActionType (Composition). */
  autoActionTypeId?: number;
};

export type CommandTypeNode = CommandTypeCategory | CommandTypeGroup | CommandTypeLeaf;

export type CommandCategoryPath = {
  l1Id: string;
  l1Label: string;
  l2Id?: string;
  l2Label?: string;
  actionTypeId: number;
  display: string;
};

function leaf(actionTypeId: number): CommandTypeLeaf {
  // Resolve display at call time (and again in UI) so module-init order cannot leave blank labels.
  return {
    kind: 'type',
    actionTypeId,
    get display() {
      return getTransactionCommandActionTypeDisplay(actionTypeId) || String(actionTypeId);
    },
  } as CommandTypeLeaf;
}

/**
 * Full Master–Detail taxonomy (before context / exclusion filters).
 * L1 order: Composition → Form & Data → Messaging → API → Import & File → UI.
 * Form & Data / Import & File are flat L1→leaf (former L2 groups removed or promoted).
 */
export const COMMAND_TYPE_TAXONOMY_ROOT: CommandTypeCategory[] = [
  {
    kind: 'category',
    id: 'composition',
    label: 'Composition',
    autoActionTypeId: 200,
    children: [leaf(200)],
  },
  {
    kind: 'category',
    id: 'data',
    /** Save / refresh / validate / formulas / SQL / data transfer (not UI chrome, not import). */
    label: 'Form & Data Operations',
    children: [
      leaf(49),
      leaf(50),
      leaf(205),
      leaf(44),
      leaf(57),
      leaf(45),
      leaf(46),
      leaf(42),
      leaf(43),
    ],
  },
  {
    kind: 'category',
    id: 'messaging',
    label: 'Messaging',
    children: [leaf(60), leaf(61), leaf(62), leaf(63)],
  },
  {
    kind: 'category',
    id: 'apiIntegration',
    /** Call API / Plugin / External Method / External Exe */
    label: 'API, Plugin & External Process',
    children: [leaf(59), leaf(40), leaf(38), leaf(68)],
  },
  {
    kind: 'category',
    id: 'importFile',
    label: 'Import & File Processing',
    children: [
      leaf(66),
      leaf(67),
      leaf(71),
      leaf(72),
      leaf(76),
      leaf(77),
      leaf(73),
      leaf(81),
      leaf(82),
    ],
  },
  {
    kind: 'category',
    id: 'ui',
    label: 'UI Operations',
    // Save As is a UI dialog/window action (Angular Display still "UI: Save As")
    children: [leaf(54), leaf(53), leaf(48), leaf(74), leaf(47)],
  },
];

const LIST_EDIT_ALLOWED = new Set<number>([43, 49, 50, 200]);

/** UI:* types excluded from workflow embedded editor (Angular parity + no Create User). */
const WORKFLOW_EMBEDDED_UI_TYPE_IDS = new Set<number>([47, 48, 51, 53, 54, 55, 74]);

function collectLeaves(nodes: readonly CommandTypeNode[]): CommandTypeLeaf[] {
  const out: CommandTypeLeaf[] = [];
  const walk = (n: CommandTypeNode) => {
    if (n.kind === 'type') {
      out.push(n);
      return;
    }
    for (const c of n.children) walk(c as CommandTypeNode);
  };
  for (const n of nodes) walk(n);
  return out;
}

function filterLeavesByAllowed(
  nodes: CommandTypeNode[],
  allowed: Set<number> | null
): CommandTypeNode[] {
  const filterNode = (n: CommandTypeNode): CommandTypeNode | null => {
    if (n.kind === 'type') {
      if (COMMAND_ACTION_TYPE_PICKER_EXCLUDED_IDS.has(n.actionTypeId)) return null;
      if (allowed && !allowed.has(n.actionTypeId)) return null;
      return n;
    }
    if (n.kind === 'group') {
      const children = n.children
        .map((c) => filterNode(c))
        .filter((c): c is CommandTypeLeaf => c != null && c.kind === 'type');
      if (children.length === 0) return null;
      return { ...n, children };
    }
    // category
    const children = (n.children as CommandTypeNode[])
      .map((c) => filterNode(c))
      .filter((c): c is CommandTypeNode => c != null);
    if (children.length === 0) return null;
    return { ...n, children };
  };

  return nodes.map(filterNode).filter((n): n is CommandTypeNode => n != null);
}

export function resolveCommandTypeTaxonomyContext(
  transactionOrganizedType: number | null | undefined,
  options?: { workflowEmbedded?: boolean }
): CommandTypeTaxonomyContext {
  if (options?.workflowEmbedded) return 'workflowEmbedded';
  if (transactionOrganizedType === EmTransactionOrganizedType.List) return 'listEdit';
  return 'masterDetail';
}

export function getCommandTypeTaxonomyTree(context: CommandTypeTaxonomyContext): CommandTypeCategory[] {
  let allowed: Set<number> | null = null;
  if (context === 'listEdit') {
    allowed = LIST_EDIT_ALLOWED;
  } else if (context === 'workflowEmbedded') {
    // All taxonomy leaves except UI window/print/nav types and global exclusions
    const all = collectLeaves(COMMAND_TYPE_TAXONOMY_ROOT).map((l) => l.actionTypeId);
    allowed = new Set(all.filter((id) => !WORKFLOW_EMBEDDED_UI_TYPE_IDS.has(id)));
  }

  return filterLeavesByAllowed(
    COMMAND_TYPE_TAXONOMY_ROOT.map((c) => ({ ...c, children: [...c.children] })),
    allowed
  ) as CommandTypeCategory[];
}

/** Flat picker options from taxonomy (Display order preserved within groups; not A–Z flat). */
export function getCommandTypeTaxonomyFlatOptions(
  context: CommandTypeTaxonomyContext
): TransactionCommandActionTypeOption[] {
  return collectLeaves(getCommandTypeTaxonomyTree(context)).map((l) => ({
    Id: l.actionTypeId,
    Display: l.display,
  }));
}

export function isCommandActionTypeExcludedFromPicker(actionTypeId: number): boolean {
  return COMMAND_ACTION_TYPE_PICKER_EXCLUDED_IDS.has(actionTypeId);
}

/** Filter legacy flat lists (51/55/70). Prefer taxonomy flat options for new UI. */
export function filterCommandActionTypeOptionsForPicker(
  list: readonly TransactionCommandActionTypeOption[]
): TransactionCommandActionTypeOption[] {
  return list.filter((x) => !COMMAND_ACTION_TYPE_PICKER_EXCLUDED_IDS.has(Number(x?.Id)));
}

function buildPathIndex(): Map<number, CommandCategoryPath> {
  const map = new Map<number, CommandCategoryPath>();
  for (const cat of COMMAND_TYPE_TAXONOMY_ROOT) {
    for (const child of cat.children) {
      if (child.kind === 'type') {
        map.set(child.actionTypeId, {
          l1Id: cat.id,
          l1Label: cat.label,
          actionTypeId: child.actionTypeId,
          display: child.display,
        });
      } else if (child.kind === 'group') {
        for (const t of child.children) {
          map.set(t.actionTypeId, {
            l1Id: cat.id,
            l1Label: cat.label,
            l2Id: child.id,
            l2Label: child.label,
            actionTypeId: t.actionTypeId,
            display: t.display,
          });
        }
      }
    }
  }
  return map;
}

const PATH_BY_ACTION_TYPE = buildPathIndex();

export function getCategoryPath(actionTypeId: number | null | undefined): CommandCategoryPath | null {
  if (actionTypeId == null) return null;
  return PATH_BY_ACTION_TYPE.get(Number(actionTypeId)) ?? null;
}

export function formatCategoryPathBreadcrumb(actionTypeId: number | null | undefined): string {
  const p = getCategoryPath(actionTypeId);
  if (!p) return '';
  return p.l2Label ? `${p.l1Label} / ${p.l2Label}` : p.l1Label;
}

/**
 * Show Category readonly field for any known type except Composition
 * (L1 auto-select — Category would only repeat "Composition").
 */
export function shouldShowCommandCategoryField(actionTypeId: number | null | undefined): boolean {
  const p = getCategoryPath(actionTypeId);
  if (!p) return false;
  if (p.l1Id === 'composition') return false;
  return true;
}

/** Search: match leaf display or ancestor labels; returns leaves with ancestors expanded conceptually. */
export function searchCommandTypeTaxonomyLeaves(
  tree: readonly CommandTypeCategory[],
  query: string
): CommandTypeLeaf[] {
  const q = query.trim().toLowerCase();
  if (!q) return collectLeaves(tree);

  const hits: CommandTypeLeaf[] = [];
  for (const cat of tree) {
    const catHit = cat.label.toLowerCase().includes(q);
    for (const child of cat.children) {
      if (child.kind === 'type') {
        if (
          catHit ||
          child.display.toLowerCase().includes(q) ||
          String(child.actionTypeId).includes(q)
        ) {
          hits.push(child);
        }
      } else if (child.kind === 'group') {
        const groupHit = child.label.toLowerCase().includes(q);
        for (const t of child.children) {
          if (
            catHit ||
            groupHit ||
            t.display.toLowerCase().includes(q) ||
            String(t.actionTypeId).includes(q)
          ) {
            hits.push(t);
          }
        }
      }
    }
  }
  return hits;
}

/** Filter tree to only branches that contain at least one search hit (for tree UI while searching). */
export function filterCommandTypeTaxonomyTreeBySearch(
  tree: readonly CommandTypeCategory[],
  query: string
): CommandTypeCategory[] {
  const q = query.trim().toLowerCase();
  if (!q) return [...tree];

  const out: CommandTypeCategory[] = [];
  for (const cat of tree) {
    const catHit = cat.label.toLowerCase().includes(q);
    const nextChildren: Array<CommandTypeGroup | CommandTypeLeaf> = [];

    for (const child of cat.children) {
      if (child.kind === 'type') {
        if (
          catHit ||
          child.display.toLowerCase().includes(q) ||
          String(child.actionTypeId).includes(q)
        ) {
          nextChildren.push(child);
        }
      } else if (child.kind === 'group') {
        const groupHit = child.label.toLowerCase().includes(q);
        const leaves = child.children.filter(
          (t) =>
            catHit ||
            groupHit ||
            t.display.toLowerCase().includes(q) ||
            String(t.actionTypeId).includes(q)
        );
        if (leaves.length > 0) {
          nextChildren.push({ ...child, children: leaves });
        }
      }
    }

    if (nextChildren.length > 0 || (cat.autoActionTypeId != null && catHit)) {
      out.push({ ...cat, children: nextChildren });
    }
  }
  return out;
}
