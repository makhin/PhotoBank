import React from "react";
import { View, Text, Image, StyleSheet, TouchableOpacity } from "react-native";
import { PhotoItemDto } from "@photobank/shared/types";

export const PhotoCard = ({ photo }: { photo: PhotoItemDto }) => {
  return (
    <TouchableOpacity style={styles.card}>
      <Image
        source={{ uri: `data:image/jpeg;base64,${photo.thumbnail}` }}
        style={styles.image}
      />
      <Text style={styles.title}>{photo.name}</Text>
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  card: {
    margin: 5,
    backgroundColor: "#222",
    borderRadius: 8,
    overflow: "hidden",
  },
  image: {
    width: 200,
    height: 120,
  },
  title: {
    color: "#fff",
    textAlign: "center",
    padding: 5,
  },
});
