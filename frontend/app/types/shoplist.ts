export interface ShoplistItem {
  id: string;
  name: string;
  done: boolean;
  position: number;
}

export interface Shoplist {
  id: string;
  name: string;
  items: ShoplistItem[];
}
