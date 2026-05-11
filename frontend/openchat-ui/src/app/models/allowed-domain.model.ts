export interface AllowedDomain {
  id: string;
  domain: string;
  enabled: boolean;
  category: DomainCategory;
  description: string;
  allowSubdomains: boolean;
  addedBy: string;
  isSystemDefault: boolean;
  addedAt: string;
  updatedAt: string;
}

export interface AllowedDomainRequest {
  domain: string;
  category: DomainCategory;
  description: string;
  allowSubdomains: boolean;
  enabled: boolean;
}

export type DomainCategory =
  | 'framework_docs'
  | 'platform_docs'
  | 'language_docs'
  | 'web_docs'
  | 'community'
  | 'code_hosting'
  | 'reference'
  | 'custom';

export const CATEGORY_LABELS: Record<DomainCategory, string> = {
  framework_docs: 'Framework Docs',
  platform_docs:  'Platform Docs',
  language_docs:  'Language Docs',
  web_docs:       'Web Docs',
  community:      'Community',
  code_hosting:   'Code Hosting',
  reference:      'Reference',
  custom:         'Custom',
};

export const CATEGORY_COLORS: Record<DomainCategory, string> = {
  framework_docs: 'bg-purple-500/10 text-purple-400 border border-purple-500/30',
  platform_docs:  'bg-blue-500/10 text-blue-400 border border-blue-500/30',
  language_docs:  'bg-indigo-500/10 text-indigo-400 border border-indigo-500/30',
  web_docs:       'bg-cyan-500/10 text-cyan-400 border border-cyan-500/30',
  community:      'bg-amber-500/10 text-amber-400 border border-amber-500/30',
  code_hosting:   'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30',
  reference:      'bg-neutral-500/10 text-neutral-400 border border-neutral-500/30',
  custom:         'bg-rose-500/10 text-rose-400 border border-rose-500/30',
};

export const ALL_CATEGORIES: DomainCategory[] = [
  'framework_docs', 'platform_docs', 'language_docs', 'web_docs',
  'community', 'code_hosting', 'reference', 'custom',
];
