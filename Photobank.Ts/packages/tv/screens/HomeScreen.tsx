import React, { useState } from "react";
import { View, StyleSheet } from "react-native";
import { FilterDto } from "@photobank/shared/types";
import { PhotoGrid } from "../components/PhotoGrid";
import { PhotoFilters } from "../components/PhotoFilters";

export const HomeScreen = () => {
  const [filter, setFilter] = useState<FilterDto | null>(null);

  return (
    <View style={styles.container}>
      <PhotoFilters onApply={setFilter} />
      <PhotoGrid filter={filter} />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#000", // Тёмный фон для TV
  },
});
