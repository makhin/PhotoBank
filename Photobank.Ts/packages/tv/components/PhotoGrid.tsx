import React from "react";
import { View, FlatList, StyleSheet, Text } from "react-native";
import { FilterDto } from "@photobank/shared/types";
import { PhotoCard } from "./PhotoCard";
import { usePhotos } from "../hooks/usePhotoApi";

export const PhotoGrid = ({ filter }: { filter: FilterDto | null }) => {
  const { photos, loading } = usePhotos(filter);

  return (
    <View style={styles.container}>
      {loading ? (
        <Text style={styles.loading}>Загрузка...</Text>
      ) : (
        <FlatList
          data={photos}
          renderItem={({ item }) => <PhotoCard photo={item} />}
          keyExtractor={(item) => item.id.toString()}
          numColumns={3}
        />
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1 },
  loading: { color: "#fff", textAlign: "center", marginTop: 20 },
});
