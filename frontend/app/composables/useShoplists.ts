import { useMutation, useQuery, useQueryClient } from "@tanstack/vue-query";
import { useToast } from "primevue/usetoast";
import { shoplistKeys } from "~/composables/queryKeys";

export function useShoplists() {
  const api = useApi();
  const toast = useToast();
  const queryClient = useQueryClient();

  const { data, isPending, isError, suspense } = useQuery({
    queryKey: shoplistKeys.all,
    queryFn: async () => {
      const { data, error } = await api.GET("/shoplists");
      if (error)
        throw error;
      return data;
    },
  });

  onServerPrefetch(async () => {
    await suspense();
  });

  const createListMutation = useMutation({
    mutationFn: async (name: string) => {
      const { data, error } = await api.POST("/shoplists", {
        body: { name },
      });
      if (error)
        throw error;
      return data;
    },
    onSuccess: () => {
      // `void` is intentional: we fire-and-forget the invalidation so the
      // mutation settles as soon as the HTTP call succeeds. If we returned
      // (or awaited) the promise, Vue Query would keep the mutation in its
      // pending state until the triggered refetch completes — making the UI
      // feel slower for no benefit here. The refetch still happens in the
      // background and reconciles the cache shortly after. Same pattern is
      // applied to every onSuccess in this file.
      void queryClient.invalidateQueries({ queryKey: shoplistKeys.all, exact: true });
    },
    onError: () => {
      toast.add({ severity: "error", summary: "Failed to create list", life: 3000 });
    },
  });

  const createList = (name: string) =>
    createListMutation.mutateAsync(name).then(r => r.id);

  return {
    lists: data,
    isPending,
    isError,
    createList,
    isCreatingList: createListMutation.isPending,
  };
}
