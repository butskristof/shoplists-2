export const shoplistKeys = {
  all: ["shoplists"] as const,
  detail: (id: string) => ["shoplists", id] as const,
};
