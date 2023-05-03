use std::collections::HashMap;
use std::hash::Hash;
use std::str::FromStr;

use bimap::{BiHashMap, Overwritten};
use cgmath::Vector3;
use serde::{Deserialize, Serialize};

pub type Coord = Vector3<i32>;

pub trait Value: Hash + Eq + Default + Clone {}

impl<T: Hash + Eq + Default + Clone> Value for T {}

/// Grid data storage type for serialization/deserialization
#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct GridData<T: Value>
{
    voxels: HashMap<String, T>,
}

impl<T: Value> From<Grid<T>> for GridData<T>
{
    fn from(value: Grid<T>) -> Self {
        Self {
            voxels: value.voxels
                .iter()
                .map(|(left, right)|
                    (format!("{},{},{}", left.x, left.y, left.z), right.clone()))
                .collect(),
        }
    }
}

impl<T: Value> From<GridData<T>> for Grid<T>
{
    fn from(value: GridData<T>) -> Self {
        Self {
            voxels: value.voxels
                .into_iter()
                .map(|(k, v)| {
                    let e = k.split(",").map(|e| i32::from_str(e).unwrap()).collect::<Vec<i32>>();
                    (Coord::new(e[0], e[1], e[2]), v)
                })
                .collect(),
        }
    }
}

/// Stores a voxel grid
///
/// NOTE: Values of type `T` MUST be globally unique (and hash uniquely)
#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct Grid<T: Value>
{
    voxels: BiHashMap<Coord, T>,
}

impl<T: Value> Grid<T>
{
    pub fn contains(&self, location: Coord) -> bool {
        self.voxels.contains_left(&location)
    }

    /// Get the voxel value at the given location
    pub fn get(&self, location: Coord) -> Option<&T> {
        self.voxels.get_by_left(&location)
    }

    /// Remove the voxel value at the given location
    pub fn remove(&mut self, location: Coord) -> Option<T> {
        self.voxels.remove_by_left(&location).map(|e| e.1)
    }

    /// Update the voxel value at the given coordinate using the given function
    ///
    /// Returns whether or not the element was modified (i.e. if it existed)
    pub fn _update(&mut self, location: Coord, f: fn(&mut T)) -> bool {
        if let Some(v) = self.get(location) {
            let mut value = v.clone();
            f(&mut value);
            self.set(location, value);
            true
        } else {
            false
        }
    }

    /// Set the voxel value at the given location
    ///
    /// Returns true if successful, false if the value is a duplicate
    pub fn set(&mut self, location: Coord, value: T) -> bool {
        if self.voxels.contains_right(&value) {
            false
        } else {
            match self.voxels.insert(location, value) {
                Overwritten::Neither | Overwritten::Left(_, _) => true,
                _ => panic!("voxel value was not globally unique")
            }
        }
    }

    /// Get all adjacent voxels (if they exist)
    pub fn get_adjacent(&self, location: Coord) -> Vec<(Coord, &T)> {
        let offsets = vec![
            Vector3::new(-1, 0, 0),
            Vector3::new(1, 0, 0),
            Vector3::new(0, -1, 0),
            Vector3::new(0, 1, 0),
            Vector3::new(0, 0, -1),
            Vector3::new(0, 0, 1),
        ];

        offsets
            .iter()
            .map(|e| *e + location)
            .filter_map(|e| self.get(e).map(|v| (e, v)))
            .collect()
    }
}