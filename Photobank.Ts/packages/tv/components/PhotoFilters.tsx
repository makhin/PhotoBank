import React, { useState } from "react";
import { View, Text, Button, StyleSheet } from "react-native";
import DateTimePicker, { DateTimePickerEvent } from "@react-native-community/datetimepicker";
import type { ComponentType } from "react";
import { FilterDto } from "@photobank/shared/types";
import { DEFAULT_PHOTO_FILTER } from '@photobank/shared/constants';

export const PhotoFilters = ({
  onApply,
}: {
  onApply: (f: FilterDto) => void;
}) => {
  const [filter, setFilter] = useState<FilterDto>(DEFAULT_PHOTO_FILTER);
  const [showDatePicker, setShowDatePicker] = useState(false);
  const Picker = DateTimePicker as unknown as ComponentType<any>;

  const applyFilter = () => {
    onApply(filter);
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Фильтры</Text>
      <Button title="Выбрать дату" onPress={() => setShowDatePicker(true)} />
      {showDatePicker && (
        <Picker
          value={new Date()}
          mode="date"
          display="default"
          onChange={(e: DateTimePickerEvent, date?: Date) => {
            setShowDatePicker(false);
            setFilter({ ...filter, takenDateFrom: date?.toISOString() });
          }}
        />
      )}
      <Button title="Применить" onPress={applyFilter} />
    </View>
  );
};

const styles = StyleSheet.create({
  container: { padding: 20 },
  title: { fontSize: 24, color: "#fff", marginBottom: 10 },
});
