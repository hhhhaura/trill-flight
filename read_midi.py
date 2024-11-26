import json
from mido import MidiFile

def parse_midi_to_json(midi_file_path, output_file_path):
    midi = MidiFile(midi_file_path)
    note_events = []
    active_notes = {}  # 用於追踪每個 note 的啟動時間

    current_time = 0  # 用於追踪當前時間
    for track in midi.tracks:
        for msg in track:
            current_time += msg.time

            if msg.type == 'note_on' and msg.velocity > 0:  # Note On 事件
                # 將音符加入追踪字典
                active_notes[msg.note] = current_time / midi.ticks_per_beat

            elif (msg.type == 'note_off' or (msg.type == 'note_on' and msg.velocity == 0)):
                # 處理 Note Off 或 Note On (velocity 為 0)
                if msg.note in active_notes:
                    start_time = active_notes.pop(msg.note)
                    duration = (current_time / midi.ticks_per_beat) - start_time
                    note_events.append({
                        "note": msg.note,  # 音高 (MIDI 編碼)
                        "time": start_time,  # 開始時間 (以拍子為單位)
                        "duration": duration,  # 持續時間
                    })

    # 將結果保存為 JSON 文件
    with open(output_file_path, "w") as f:
        json.dump(note_events, f, indent=4)
    print(f"Exported to {output_file_path}")

# 示例用法
parse_midi_to_json("testMidi.mid", "output2.json")
