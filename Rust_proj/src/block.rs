use std::collections::HashMap;
use std::fmt::{Display, Formatter};

use cgmath::{Array, Vector3};
use serde::{Deserialize, Serialize};

pub type VoxelID = String;
pub type Color = (u8, u8, u8, u8);

pub const MANHATTAN_ADJACENT_CUBE: usize = 6;
pub const VOXEL_CIRCUIT: &str = "!";

pub fn manhattan_distance_ref(a: &[f32], b: &[f32]) -> f32 {
    manhattan_distance(GridCoord::from(a), GridCoord::from(b)) as f32
}

pub fn manhattan_distance(a: GridCoord, b: GridCoord) -> i32 {
    (Vector3::<f32>::from(a.0) - Vector3::<f32>::from(b.0)).map(|c| c.abs()).sum() as i32
}

pub fn is_adjacent(a: GridCoord, b: GridCoord) -> bool {
    manhattan_distance(a, b) == 1
}

#[derive(Clone, Copy, Debug, Serialize, Deserialize)]
pub struct GridCoord(pub [f32; 3]);

impl Default for GridCoord
{
    fn default() -> Self {
        Self::zero()
    }
}

impl Into<Vector3<f32>> for GridCoord
{
    fn into(self) -> Vector3<f32> {
        Vector3::new(self.0[0], self.0[1], self.0[2])
    }
}

impl From<Vector3<f32>> for GridCoord
{
    fn from(value: Vector3<f32>) -> Self {
        Self::newf(value.x, value.y, value.z)
    }
}

impl From<&[f32]> for GridCoord {
    fn from(value: &[f32]) -> Self {
        Self([value[0], value[1], value[2]])
    }
}

impl AsRef<[f32]> for GridCoord
{
    fn as_ref(&self) -> &[f32] {
        // <Vector3<f32> as AsRef<[f32; 3]>>::as_ref(&self.0).into()
        &self.0
    }
}

impl PartialEq for GridCoord
{
    fn eq(&self, other: &Self) -> bool {
        self.0 == other.0
    }
}

impl GridCoord
{
    pub fn zero() -> Self {
        Self::new(0, 0, 0)
    }

    pub fn newf(x: f32, y: f32, z: f32) -> Self {
        Self([x, y, z])
    }

    pub fn new(x: i32, y: i32, z: i32) -> Self {
        Self::newf(x as f32, y as f32, z as f32)
    }
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub enum BlockRotation
{
    FORWARD,
    RIGHT,
    LEFT,
    BACKWARD,
    UPWARD,
    DOWNWARD,
}

impl Default for BlockRotation
{
    fn default() -> Self {
        Self::FORWARD
    }
}

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct VoxelPowered
{
    powered: bool,
}

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct VoxelBlock
{
    color: Color,
}

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct VoxelClock
{
    frequency: u32,
    start_tick: u32,
}

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct VoxelPulse
{
    powered: bool,
    pulse_ticks: u32,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
#[serde(tag = "block", content = "data")]
pub enum Block
{
    Air,
    Wire(VoxelPowered),
    Block(VoxelBlock),
    Toggle(VoxelPowered),
    Pixel(VoxelPowered),
    ANDGate(VoxelPowered),
    ORGate(VoxelPowered),
    NOTGate(VoxelPowered),
    Clock(VoxelClock),
    Pulse(VoxelPulse),
}

impl Display for Block
{
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.get_name())
    }
}

impl Default for Block
{
    fn default() -> Self {
        Self::Air
    }
}

impl Block
{
    /// Return the voxels that make up the block
    /// Voxels whose names start with an exclamation are considered as part of the circuit
    pub fn get_structure(&self) -> HashMap<VoxelID, GridCoord> {
        match self {
            Block::Air => Default::default(),
            Block::Wire(_) => [("".to_string(), GridCoord::zero())].into(),
            Block::Block(_) => [("".to_string(), GridCoord::zero())].into(),
            Block::Toggle(_) => [("".to_string(), GridCoord::zero())].into(),
            Block::Pixel(_) => [("".to_string(), GridCoord::zero())].into(),
            Block::ANDGate(_) => [
                (format!("{}in_a", VOXEL_CIRCUIT), GridCoord::new(-1, 1, 0)),
                (format!("{}in_b", VOXEL_CIRCUIT), GridCoord::new(-1, -1, 0)),
                (format!("{}out", VOXEL_CIRCUIT), GridCoord::new(1, 0, 0)),
            ].into(),
            Block::ORGate(_) => [
                (format!("{}in_a", VOXEL_CIRCUIT), GridCoord::new(-1, 1, 0)),
                (format!("{}in_b", VOXEL_CIRCUIT), GridCoord::new(-1, -1, 0)),
                (format!("{}out", VOXEL_CIRCUIT), GridCoord::new(1, 0, 0)),
            ].into(),
            Block::NOTGate(_) => [
                (format!("{}in", VOXEL_CIRCUIT), GridCoord::new(-1, 0, 0)),
                (format!("{}out", VOXEL_CIRCUIT), GridCoord::new(1, 0, 0)),
            ].into(),
            Block::Clock(_) => [
                (format!("{}out", VOXEL_CIRCUIT), GridCoord::zero()),
            ].into(),
            Block::Pulse(_) => [
                (format!("{}out", VOXEL_CIRCUIT), GridCoord::zero()),
            ].into(),
        }
    }

    pub fn get_name(&self) -> &str {
        match self {
            Block::Air => "air",
            Block::Wire(_) => "wire",
            Block::Block(_) => "block",
            Block::Toggle(_) => "toggle",
            Block::Pixel(_) => "pixel",
            Block::ANDGate(_) => "gate_and",
            Block::ORGate(_) => "gate_or",
            Block::NOTGate(_) => "gate_not",
            Block::Clock(_) => "clock",
            Block::Pulse(_) => "pulse",
        }
    }
}