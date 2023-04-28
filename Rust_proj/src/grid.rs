use std::hash::Hash;

use bimap::{BiHashMap, Overwritten};
use cgmath::Vector3;
use serde::{Deserialize, Serialize};

pub type Coord = Vector3<i32>;

pub trait Value: Hash + Eq + Default + Clone {}

impl<T: Hash + Eq + Default + Clone> Value for T {}

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